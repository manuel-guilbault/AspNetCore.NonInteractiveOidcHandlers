using System;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	public abstract class CachingTokenProvider: ITokenProvider
	{
		private readonly ILogger<CachingTokenProvider> _logger;
		private readonly IDistributedCache _cache;
		private readonly CachingOptions _options;

		protected CachingTokenProvider(
			ILogger<CachingTokenProvider> logger, 
			IDistributedCache cache, 
			CachingOptions options)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_cache = cache;
			_options = options ?? throw new ArgumentNullException(nameof(options));
		}
		
		protected async Task<TokenResponse> GetTokenAsync(string cacheKey, Func<CancellationToken, Task<TokenResponse>> getToken, CancellationToken cancellationToken)
		{
			if (!_options.EnableCaching)
			{
				return await getToken(cancellationToken).ConfigureAwait(false);
			}

			var prefixedCacheKey = _options.CacheKeyPrefix + _options.HttpClientName + ":" + cacheKey;

			var cachedDelegatedTokenResponse = await _cache.GetTokenAsync(prefixedCacheKey, cancellationToken).ConfigureAwait(false);
			if (cachedDelegatedTokenResponse != null)
			{
				_logger.LogTrace("Token found in cache.");
				return cachedDelegatedTokenResponse;
			}

			_logger.LogTrace("Token is not cached.");

			var tokenResponse = await getToken(cancellationToken).ConfigureAwait(false);
			await _cache
				.SetTokenAsync(prefixedCacheKey, tokenResponse, _options, cancellationToken)
				.ConfigureAwait(false);
			return tokenResponse;
		}

		public abstract Task<TokenResponse> GetTokenAsync(CancellationToken cancellationToken);
	}
}
