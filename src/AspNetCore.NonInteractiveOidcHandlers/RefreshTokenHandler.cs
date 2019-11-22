using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	public class RefreshTokenHandler: CachingTokenHandler
	{
		private readonly ILogger<RefreshTokenHandler> _logger;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly RefreshTokenHandlerOptions _options;
		private readonly IServiceProvider _serviceProvider;

		public RefreshTokenHandler(
			ILogger<RefreshTokenHandler> logger,
			IHttpClientFactory httpClientFactory,
			IDistributedCache cache,
			RefreshTokenHandlerOptions options,
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
			var refreshToken = _options.RefreshTokenRetriever(_serviceProvider);
			if (refreshToken.IsMissing())
			{
				_logger.LogTrace($"No current refresh token.");
				return null;
			}

			return await GetTokenAsync($"refresh_token:{refreshToken.ToSha512()}", _ => AcquireTokenAsync(refreshToken), cancellationToken)
				.ConfigureAwait(false);
		}

		private async Task<TokenResponse> AcquireTokenAsync(string refreshToken)
		{
			var lazyToken = _options.LazyTokens.GetOrAdd(refreshToken, rt => new AsyncLazy<TokenResponse>(() => RequestTokenAsync(rt)));
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
				// If caching is on and it succeeded, the token is now in the cache.
				// If caching is off and it succeeded, the token will be discarded.
				// Either way, we want to remove the temporary store of token for this token because it is only intended for de-duping fetch requests
				_options.LazyTokens.TryRemove(refreshToken, out _);
			}
		}

		private async Task<TokenResponse> RequestTokenAsync(string refreshToken)
		{
			var httpClient = _httpClientFactory.CreateClient(_options.AuthorityHttpClientName);
			var tokenEndpoint = await _options.GetTokenEndpointAsync(httpClient).ConfigureAwait(false);
			var tokenRequest = new RefreshTokenRequest
			{
				Address = tokenEndpoint,
				GrantType = _options.GrantType,
				ClientId = _options.ClientId,
				ClientSecret = _options.ClientSecret,
				RefreshToken = refreshToken
			};
			tokenRequest.Parameters.AddRange(_options.ExtraTokenParameters);
			var tokenResponse = await httpClient.RequestRefreshTokenAsync(tokenRequest).ConfigureAwait(false);
			return tokenResponse;
		}
	}
}
