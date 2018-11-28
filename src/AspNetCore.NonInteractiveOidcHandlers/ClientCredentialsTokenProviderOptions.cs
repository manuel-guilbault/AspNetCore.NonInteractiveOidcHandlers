using System.Collections.Generic;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel;
using IdentityModel.Client;

namespace AspNetCore.NonInteractiveOidcHandlers
{
    public class ClientCredentialsTokenProviderOptions: TokenProviderOptions
    {
        /// <summary>
        /// The grant type used for the token request (default: client_credentials).
        /// </summary>
        public string GrantType { get; set; } = OidcConstants.GrantTypes.ClientCredentials;

        /// <summary>
        /// Specifies the scope of the requested tokens (required).
        /// </summary>
        public string Scope { get; set; }

        /// <summary>
        /// Extra parameters passed to the token request.
        /// </summary>
        public IDictionary<string, string> ExtraTokenParameters { get; set; }

        internal AsyncMutex<TokenResponse> TokenMutex { get; set; }

        protected override IEnumerable<string> GetValidationErrors()
        {
            foreach (var error in base.GetValidationErrors()) yield return error;

            if (Scope.IsMissing())
            {
                yield return $"You must set {nameof(Scope)}.";
            }

            if (GrantType.IsMissing())
            {
                yield return $"You must set {nameof(GrantType)}.";
            }
        }
    }
}
