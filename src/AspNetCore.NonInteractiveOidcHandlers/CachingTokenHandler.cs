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
	public abstract class CachingTokenHandler: DelegatingHandler
	{
		private readonly ILogger<CachingTokenHandler> _logger;
		private readonly IDistributedCache _cache;
		private readonly CachingOptions _options;

		protected CachingTokenHandler(
			ILogger<CachingTokenHandler> logger, 
			IDistributedCache cache, 
			CachingOptions options)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_cache = cache;
			_options = options ?? throw new ArgumentNullException(nameof(options));
		}
		
		protected async Task<TokenResponse> GetTokenAsync(string cacheKey, Func<CancellationToken, Task<TokenResponse>> requestToken, CancellationToken cancellationToken)
		{
			if (!_options.EnableCaching)
			{
				return await requestToken(cancellationToken).ConfigureAwait(false);
			}

			var prefixedCacheKey = _options.CacheKeyPrefix + _options.HttpClientName + ":" + cacheKey;

			var cachedDelegatedTokenResponse = await _cache.GetTokenAsync(prefixedCacheKey, cancellationToken).ConfigureAwait(false);
			if (cachedDelegatedTokenResponse != null)
			{
				_logger.LogTrace("Token found in cache.");
				return cachedDelegatedTokenResponse;
			}

			_logger.LogTrace("Token is not cached.");

			var tokenResponse = await requestToken(cancellationToken).ConfigureAwait(false);
			if (tokenResponse != null && !tokenResponse.IsError)
			{
				await _cache
					.SetTokenAsync(prefixedCacheKey, tokenResponse, _options, cancellationToken)
					.ConfigureAwait(false);
			}

			return tokenResponse;
		}

		public abstract Task<TokenResponse> GetTokenAsync(CancellationToken cancellationToken);

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var token = await GetTokenAsync(cancellationToken);
			if (token != null && !token.IsError && token.AccessToken.IsPresent())
			{
				request.SetBearerToken(token.AccessToken);
			}

			return await base.SendAsync(request, cancellationToken);
		}
	}
}
