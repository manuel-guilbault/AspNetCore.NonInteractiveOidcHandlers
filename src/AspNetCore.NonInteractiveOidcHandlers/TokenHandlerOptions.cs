using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;

namespace AspNetCore.NonInteractiveOidcHandlers
{
    public class TokenHandlerOptions: CachingOptions, IValidatableOptions
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
        /// Gets or sets the <see cref="TokenHandlerEvents"/> used to handle token request events.
        /// </summary>
        public TokenHandlerEvents Events { get; set; } = new TokenHandlerEvents();

        internal DiscoveryCache DiscoveryCache { get; set; }

        public virtual IEnumerable<string> GetValidationErrors()
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
