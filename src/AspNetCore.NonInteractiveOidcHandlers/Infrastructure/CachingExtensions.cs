using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Distributed;

namespace AspNetCore.NonInteractiveOidcHandlers.Infrastructure
{
	internal static class CachingExtensions
	{
		private static readonly Encoding CacheEncoding = Encoding.UTF8;

		public static async Task<TokenResponse> GetTokenAsync(this IDistributedCache cache, string key, CancellationToken cancellationToken = default(CancellationToken))
		{
			var bytes = await cache
				.GetAsync(key, cancellationToken)
				.ConfigureAwait(false);
			if (bytes == null)
			{
				return null;
			}

			var json = CacheEncoding.GetString(bytes);
			var tokenResponse = new CachedTokenResponse(json);
			return tokenResponse;
		}

		public static async Task SetTokenAsync(this IDistributedCache cache, string key, TokenResponse tokenResponse, CachingOptions options, CancellationToken cancellationToken = default(CancellationToken))
		{
			var expiresIn = TimeSpan.FromSeconds(tokenResponse.ExpiresIn).Subtract(options.TokenExpirationDelay);
			if (expiresIn <= TimeSpan.Zero)
			{
				return;
			}

			var absoluteExpiration = DateTimeOffset.UtcNow.Add(expiresIn < options.CacheDuration ? expiresIn : options.CacheDuration);

			var json = tokenResponse.Raw;
			var bytes = CacheEncoding.GetBytes(json);
			await cache
				.SetAsync(key, bytes, new DistributedCacheEntryOptions { AbsoluteExpiration = absoluteExpiration }, cancellationToken)
				.ConfigureAwait(false);
		}
	}
}
