using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	public class RefreshTokenProvider: CachingTokenProvider, ITokenProvider
	{
		private readonly ILogger<RefreshTokenProvider> _logger;
		private readonly RefreshTokenProviderOptions _options;

		public RefreshTokenProvider(
			ILogger<RefreshTokenProvider> logger,
			IDistributedCache cache,
			RefreshTokenProviderOptions options)
			: base(logger, cache, options)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_options = options ?? throw new ArgumentNullException(nameof(options));
		}

		public override async Task<TokenResponse> GetTokenAsync(CancellationToken cancellationToken)
		{
			var refreshToken = _options.RefreshTokenRetriever();
			if (refreshToken.IsMissing())
			{
				_logger.LogTrace($"No current refresh token.");
				return null;
			}

			return await GetTokenAsync($"refresh_token:{refreshToken.ToSha512()}", ct => AcquireToken(refreshToken, ct), cancellationToken)
				.ConfigureAwait(false);
		}

		private async Task<TokenResponse> AcquireToken(string refreshToken, CancellationToken cancellationToken)
		{
			var lazyToken = _options.LazyTokens.GetOrAdd(refreshToken, _ => new AsyncLazy<TokenResponse>(() => RequestToken(refreshToken)));
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
				// If caching is on and it succeeded, the token is now in the cache.
				// If caching is off and it succeeded, the token will be discarded.
				// Either way, we want to remove the temporary store of token for this token because it is only intended for de-duping fetch requests
				_options.LazyTokens.TryRemove(refreshToken, out _);
			}
		}

		private async Task<TokenResponse> RequestToken(string refreshToken)
		{
			var httpClient = _options.AuthorityHttpClientAccessor();
			var tokenRequest = new RefreshTokenRequest
			{
				Address = await _options.GetTokenEndpointAsync().ConfigureAwait(false),
				GrantType = _options.GrantType,
				ClientId = _options.ClientId,
				ClientSecret = _options.ClientSecret,
				RefreshToken = refreshToken,
				Parameters = _options.ExtraTokenParameters ?? new Dictionary<string, string>(),
			};
			var tokenResponse = await httpClient.RequestRefreshTokenAsync(tokenRequest).ConfigureAwait(false);
			return tokenResponse;
		}
	}
}
