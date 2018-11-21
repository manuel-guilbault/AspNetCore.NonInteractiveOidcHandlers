using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using AspNetCore.OAuth2TokenDelegation.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCore.OAuth2TokenDelegation
{
    public class OAuth2TokenDelegationMessageHandler : DelegatingHandler
    {
        private readonly ILogger<OAuth2TokenDelegationMessageHandler> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly OAuth2TokenDelegationOptions _options;
        private readonly IDistributedCache _cache;

        public OAuth2TokenDelegationMessageHandler(
            ILogger<OAuth2TokenDelegationMessageHandler> logger, 
            IHttpContextAccessor httpContextAccessor, 
            IOptions<OAuth2TokenDelegationOptions> options,
            IDistributedCache cache = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _cache = cache;
        }
        
        private string GetCacheKey(string accessToken)
            => $"{_options.CacheKeyPrefix}{accessToken}:{_options.Scope}";

        private async Task<TokenResponse> GetTokenResponseAsync(CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogTrace($"No current HttpContext.");
                return null;
            }

            var token = _options.TokenRetriever(httpContext.Request);
            if (token == null)
            {
                _logger.LogInformation($"No access token in current request.");
                return null;
            }

            var cacheKey = GetCacheKey(token);

            if (_options.EnableCaching)
            {
                var cachedDelegatedTokenResponse = await _cache.GetTokenAsync(cacheKey, cancellationToken).ConfigureAwait(false);
                if (cachedDelegatedTokenResponse != null)
                {
                    _logger.LogTrace("Token found in cache.");
                    return cachedDelegatedTokenResponse;
                }

                _logger.LogTrace("Token is not cached.");
            }

            var lazyToken = _options.LazyTokens.GetOrAdd(token, CreateLazyToken);

            try
            {
                var delegatedTokenResponse = await lazyToken.Value.ConfigureAwait(false);
                if (delegatedTokenResponse.IsError)
                {
                    _logger.LogError($"Error returned from token endpoint: {delegatedTokenResponse.Error}");
                    throw new InvalidOperationException($"Token retrieval failed: {delegatedTokenResponse.Error} {delegatedTokenResponse.ErrorDescription}", delegatedTokenResponse.Exception);
                }

                if (_options.EnableCaching)
                {
                    await _cache
                        .SetTokenAsync(cacheKey, delegatedTokenResponse, _options.CacheDuration, cancellationToken)
                        .ConfigureAwait(false);
                }

                return delegatedTokenResponse;
            }
            finally
            {
                // If caching is on and it succeeded, the delegated token is now in the cache.
                // If caching is off and it succeeded, the delegated token will be discarded.
                // Either way, we want to remove the temporary store of delegated token for this token because it is only intended for de-duping fetch requests
                _options.LazyTokens.TryRemove(token, out _);
            }
        }

        private AsyncLazy<TokenResponse> CreateLazyToken(string token)
            => new AsyncLazy<TokenResponse>(() => GetDelegatedTokenFor(token));

        private async Task<TokenResponse> GetDelegatedTokenFor(string token)
        {
            var tokenClient = await _options.TokenClient.Value.ConfigureAwait(false);
            return await tokenClient
                .RequestCustomGrantAsync("delegation", _options.Scope, new { token })
                .ConfigureAwait(false);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await GetTokenResponseAsync(cancellationToken).ConfigureAwait(false);
            if (token != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
