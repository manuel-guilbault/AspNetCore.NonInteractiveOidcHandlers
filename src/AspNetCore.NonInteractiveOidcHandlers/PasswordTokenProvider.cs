using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	public class PasswordTokenProvider: CachingTokenProvider, ITokenProvider
	{
		private readonly ILogger<PasswordTokenProvider> _logger;
		private readonly PasswordTokenProviderOptions _options;
		private readonly IServiceProvider _serviceProvider;

		public PasswordTokenProvider(
			ILogger<PasswordTokenProvider> logger,
			IDistributedCache cache,
			PasswordTokenProviderOptions options,
			IServiceProvider serviceProvider)
			: base(logger, cache, options)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_options = options ?? throw new ArgumentNullException(nameof(options));
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}

		public override async Task<TokenResponse> GetTokenAsync(CancellationToken cancellationToken)
		{
			var userCredentials = _options.UserCredentialsRetriever(_serviceProvider);
			if (!userCredentials.HasValue)
			{
				_logger.LogTrace($"No current username/password.");
				return null;
			}

			var (userName, password) = userCredentials.Value;
			return await GetTokenAsync($"password:{userName}", ct => AcquireToken(userName, password, ct), cancellationToken)
				.ConfigureAwait(false);
		}

		private async Task<TokenResponse> AcquireToken(string userName, string password, CancellationToken cancellationToken)
		{
			var lazyToken = _options.LazyTokens.GetOrAdd(userName, _ => new AsyncLazy<TokenResponse>(() => RequestToken(userName, password)));
			try
			{
				var tokenResponse = await lazyToken.Value.ConfigureAwait(false);
				if (tokenResponse.IsError)
				{
					_logger.LogError($"Error returned from token endpoint: {tokenResponse.Error}");
					await _options.Events.OnTokenRequestFailed.Invoke(tokenResponse).ConfigureAwait(false);
					throw new InvalidOperationException(
						$"Token retrieval failed: {tokenResponse.Error} {tokenResponse.ErrorDescription}",
						tokenResponse.Exception);
				}

				await _options.Events.OnTokenAcquired(tokenResponse).ConfigureAwait(false);
				return tokenResponse;
			}
			finally
			{
				// If caching is on and it succeeded, the delegated token is now in the cache.
				// If caching is off and it succeeded, the delegated token will be discarded.
				// Either way, we want to remove the temporary store of delegated token for this token because it is only intended for de-duping fetch requests
				_options.LazyTokens.TryRemove(userName, out _);
			}
		}

		private async Task<TokenResponse> RequestToken(string userName, string password)
		{
			var httpClient = _options.AuthorityHttpClientAccessor();
			var tokenRequest = new PasswordTokenRequest
			{
				Address = await _options.GetTokenEndpointAsync().ConfigureAwait(false),
				GrantType = _options.GrantType,
				ClientId = _options.ClientId,
				ClientSecret = _options.ClientSecret,
				Scope = _options.Scope,
				UserName = userName,
				Password = password,
				Parameters = _options.ExtraTokenParameters ?? new Dictionary<string, string>(),
			};
			var tokenResponse = await httpClient.RequestPasswordTokenAsync(tokenRequest).ConfigureAwait(false);
			return tokenResponse;
		}
	}
}
