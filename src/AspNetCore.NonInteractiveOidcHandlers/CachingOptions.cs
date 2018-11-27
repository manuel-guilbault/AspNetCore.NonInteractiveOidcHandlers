using System;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	public class CachingOptions
	{
		/// <summary>
		/// Specifies whether caching is enabled for token delegation responses (requires a distributed cache implementation).
		/// </summary>
		public bool EnableCaching { get; set; } = false;

		/// <summary>
		/// Specifies ttl for token delegation response caches. TimeSpan.MaxValue is used by default, so the delegated token is
		/// cached as long as it is not expired.
		/// </summary>
		public TimeSpan CacheDuration { get; set; } = TimeSpan.MaxValue;

		/// <summary>
		/// Specifies how much time before the token's expiration should the cache entry expire. Used when calculating the cache entry
		/// expiration to compare with the CacheDuration property. Default to 1 minute.
		/// </summary>
		public TimeSpan TokenExpirationDelay { get; set; } = TimeSpan.FromMinutes(1);

		/// <summary>
		/// Specifies the prefix of the cache key.
		/// </summary>
		public string CacheKeyPrefix { get; set; } = "";

		internal string HttpClientName { get; set; } = "";
	}
}
