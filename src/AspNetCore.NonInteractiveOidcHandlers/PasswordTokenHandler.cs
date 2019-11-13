using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	public class PasswordTokenHandler: CachingTokenHandler
	{
		private readonly ILogger<PasswordTokenHandler> _logger;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly PasswordTokenHandlerOptions _options;
		private readonly IServiceProvider _serviceProvider;

		public PasswordTokenHandler(
			ILogger<PasswordTokenHandler> logger,
			IHttpClientFactory httpClientFactory,
			IDistributedCache cache,
			PasswordTokenHandlerOptions options,
			IServiceProvider serviceProvider)
			: base(logger, cache, options)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
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
			return await GetTokenAsync($"password:{userName}", _ => AcquireTokenAsync(userName, password), cancellationToken)
				.ConfigureAwait(false);
		}

		private async Task<TokenResponse> AcquireTokenAsync(string userName, string password)
		{
			var lazyToken = _options.LazyTokens.GetOrAdd(userName, _ => new AsyncLazy<TokenResponse>(() => RequestTokenAsync(userName, password)));
			try
			{
				var tokenResponse = await lazyToken.Value.ConfigureAwait(false);
				if (tokenResponse.IsError)
				{
					_logger.LogError($"Error returned from token endpoint: {tokenResponse.Error}");
					await _options.Events.OnTokenRequestFailed.Invoke(tokenResponse).ConfigureAwait(false);
					return tokenResponse;
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

		private async Task<TokenResponse> RequestTokenAsync(string userName, string password)
		{
			var httpClient = _httpClientFactory.CreateClient(_options.AuthorityHttpClientName);
			var tokenEndpoint = await _options.GetTokenEndpointAsync(httpClient).ConfigureAwait(false);
			var tokenRequest = new PasswordTokenRequest
			{
				Address = tokenEndpoint,
				GrantType = _options.GrantType,
				ClientId = _options.ClientId,
				ClientSecret = _options.ClientSecret,
				Scope = _options.Scope,
				UserName = userName,
				Password = password
			};
			tokenRequest.Parameters.AddRange(_options.ExtraTokenParameters);
			var tokenResponse = await httpClient.RequestPasswordTokenAsync(tokenRequest).ConfigureAwait(false);
			return tokenResponse;
		}
	}
}
