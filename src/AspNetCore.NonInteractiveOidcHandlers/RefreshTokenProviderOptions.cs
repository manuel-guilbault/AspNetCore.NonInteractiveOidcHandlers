using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel;
using IdentityModel.Client;

namespace AspNetCore.NonInteractiveOidcHandlers
{
    public class RefreshTokenProviderOptions: TokenProviderOptions
    {
        /// <summary>
        /// The grant type used for the token request (default: refresh_token).
        /// </summary>
        public string GrantType { get; set; } = OidcConstants.GrantTypes.RefreshToken;

        /// <summary>
        /// Extra parameters passed to the token request.
        /// </summary>
        public IDictionary<string, string> ExtraTokenParameters { get; set; }

        /// <summary>
        /// Specifies the method how to retrieve the refresh token to use to request an access token (required).
        /// If this function returns null, the request is considered unauthenticated and no access token will be passed to the HTTP request.
        /// </summary>
        public Func<IServiceProvider, string> RefreshTokenRetriever { get; set; }

        internal ConcurrentDictionary<string, AsyncLazy<TokenResponse>> LazyTokens { get; set; }

        protected override IEnumerable<string> GetValidationErrors()
        {
            foreach (var error in base.GetValidationErrors()) yield return error;
            
            if (GrantType.IsMissing())
            {
                yield return $"You must set {nameof(GrantType)}.";
            }

            if (RefreshTokenRetriever == null)
            {
                yield return $"You must set {nameof(RefreshTokenRetriever)}.";
            }
        }
    }
}
