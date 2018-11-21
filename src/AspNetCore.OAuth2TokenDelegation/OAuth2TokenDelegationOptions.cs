using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using IdentityModel.Client;
using AspNetCore.OAuth2TokenDelegation.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.OAuth2TokenDelegation
{
    public class OAuth2TokenDelegationOptions
    {
        public string Authority { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scope { get; set; }

        public Func<HttpRequest, string> TokenRetriever { get; set; } = TokenRetrieval.FromAuthorizationHeader();

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
        /// Specifies the prefix of the cache key.
        /// </summary>
        public string CacheKeyPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Specifies the policy for the discovery client
        /// </summary>
        public DiscoveryPolicy DiscoveryPolicy { get; set; } = new DiscoveryPolicy();

        /// <summary>
        /// Specifies the timout for contacting the discovery endpoint
        /// </summary>
        public TimeSpan DiscoveryTimeout { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Specifies the HTTP handler for the discovery endpoint
        /// </summary>
        public HttpMessageHandler DiscoveryHttpHandler { get; set; }

        /// <summary>
        /// Sets the URL of the token endpoint.
        /// If set, Authority is ignored.
        /// </summary>
        public string TokenEndpoint { get; set; }

        /// <summary>
        /// Specifies the HTTP handler for the token endpoint
        /// </summary>
        public HttpMessageHandler TokenHttpHandler { get; set; }

        public AuthenticationStyle AuthenticationStyle { get; set; } = AuthenticationStyle.BasicAuthentication;

        public TimeSpan TokenTimeout { get; set; } = TimeSpan.FromSeconds(60);

        internal AsyncLazy<TokenClient> TokenClient { get; set; }
        internal ConcurrentDictionary<string, AsyncLazy<TokenResponse>> LazyTokens { get; set; }

        public void Validate()
        {
            var validationErrors = GetValidationErrors().ToList();
            if (validationErrors.Any())
            {
                throw new InvalidOperationException($"Options are not valid:{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, validationErrors)}");
            }
        }

        private IEnumerable<string> GetValidationErrors()
        {
            if (Authority.IsMissing() && TokenEndpoint.IsMissing())
            {
                yield return $"You must either set {nameof(Authority)} or {nameof(TokenEndpoint)}.";
            }

            if (ClientId.IsMissing())
            {
                yield return $"You must set {nameof(ClientId)}.";
            }

            if (ClientSecret.IsMissing())
            {
                yield return $"You must set {nameof(ClientSecret)}.";
            }

            if (Scope.IsMissing())
            {
                yield return $"You must set {nameof(Scope)}.";
            }

            if (TokenRetriever == null)
            {
                yield return $"You must set {nameof(TokenRetriever)}.";
            }
        }
    }
}
