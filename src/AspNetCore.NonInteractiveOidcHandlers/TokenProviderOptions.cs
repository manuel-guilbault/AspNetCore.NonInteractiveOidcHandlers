using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;

namespace AspNetCore.NonInteractiveOidcHandlers
{
    public class TokenProviderOptions: CachingOptions
    {
        /// <summary>
        /// Sets the base-path of the token provider.
        /// If set, the OpenID Connect discovery document will be used to find the token endpoint.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Specifies the id of the token client (required).
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Specifies the shared secret of the token client (required).
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// A function return the HttpClient used to communicate with the OIDC authority.
        /// </summary>
        public Func<HttpClient> AuthorityHttpClientAccessor { get; set; } = () => new HttpClient();

        /// <summary>
        /// Specifies the policy for the discovery client.
        /// </summary>
        public DiscoveryPolicy DiscoveryPolicy { get; set; } = new DiscoveryPolicy();

        /// <summary>
        /// Sets the URL of the token endpoint.
        /// If set, Authority is ignored.
        /// </summary>
        public string TokenEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TokenProviderEvents"/> used to handle token request events.
        /// </summary>
        public TokenProviderEvents Events { get; set; } = new TokenProviderEvents();

        internal DiscoveryCache DiscoveryCache { get; set; }

        /// <summary>
        /// Check that the options are valid. Should throw an InvalidOperationException if things are not ok.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Validate()
        {
            var validationErrors = GetValidationErrors().ToList();
            if (validationErrors.Any())
            {
                throw new InvalidOperationException($"Options are not valid:{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, validationErrors)}");
            }
        }

        protected virtual IEnumerable<string> GetValidationErrors()
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

            if (Events == null)
            {
                yield return $"You must set {nameof(Events)}.";
            }
        }
    }
}
