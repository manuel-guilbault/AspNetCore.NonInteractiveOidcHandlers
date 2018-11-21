using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Distributed;

namespace AspNetCore.OAuth2TokenDelegation.Infrastructure
{
    public static class CachingExtensions
    {
        public static async Task<TokenResponse> GetTokenAsync(this IDistributedCache cache, string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            var bytes = await cache
                .GetAsync(key, cancellationToken)
                .ConfigureAwait(false);
            if (bytes == null)
            {
                return null;
            }

            var json = Encoding.UTF8.GetString(bytes);
            var tokenResponse = new TokenResponse(json);
            return tokenResponse;
        }

        public static async Task SetTokenAsync(this IDistributedCache cache, string key, TokenResponse tokenResponse, TimeSpan duration, CancellationToken cancellationToken = default(CancellationToken))
        {
            var expiresIn = TimeSpan.FromSeconds(tokenResponse.ExpiresIn);
            if (expiresIn <= TimeSpan.Zero)
            {
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var absoluteExpiration = now.Add(expiresIn < duration ? expiresIn : duration);

            var json = tokenResponse.Raw;
            var bytes = Encoding.UTF8.GetBytes(json);
            await cache
                .SetAsync(key, bytes, new DistributedCacheEntryOptions { AbsoluteExpiration = absoluteExpiration }, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
