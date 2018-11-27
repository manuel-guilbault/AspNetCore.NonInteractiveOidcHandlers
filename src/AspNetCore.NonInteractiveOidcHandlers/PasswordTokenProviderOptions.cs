using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel;
using IdentityModel.Client;

namespace AspNetCore.NonInteractiveOidcHandlers
{
    public class PasswordTokenProviderOptions: TokenProviderOptions
    {
        /// <summary>
        /// The grant type used for the token request (default: password).
        /// </summary>
        public string GrantType { get; set; } = OidcConstants.GrantTypes.Password;

        /// <summary>
        /// Specifies the scope of the requested tokens (required).
        /// </summary>
        public string Scope { get; set; }

        /// <summary>
        /// Specifies the method how to retrieve the username and the password to use to request an access token (required).
        /// If this function returns null, the request is considered unauthenticated and no access token will be passed to the HTTP request.
        /// </summary>
        public Func<(string userName, string password)?> UserCredentialsRetriever { get; set; }

        /// <summary>
        /// Extra parameters passed to the token request.
        /// </summary>
        public IDictionary<string, string> ExtraTokenParameters { get; set; }

        internal ConcurrentDictionary<string, AsyncLazy<TokenResponse>> LazyTokens { get; set; }

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

            if (UserCredentialsRetriever == null)
            {
                yield return $"You must set {nameof(UserCredentialsRetriever)}.";
            }
        }
    }
}
