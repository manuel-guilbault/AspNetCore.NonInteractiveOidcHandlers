using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.OAuth2TokenDelegation.Infrastructure
{
    /// <summary>
    /// Defines some common token retrieval strategies
    /// </summary>
    public static class TokenRetrieval
    {
        /// <summary>
        /// Reads the token from the authrorization header.
        /// </summary>
        /// <param name="scheme">The scheme (defaults to Bearer).</param>
        /// <returns></returns>
        public static Func<HttpRequest, string> FromAuthorizationHeader(string scheme = "Bearer")
        {
            return (request) =>
            {
                string authorization = request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrEmpty(authorization))
                {
                    return null;
                }

                if (authorization.StartsWith(scheme + " ", StringComparison.OrdinalIgnoreCase))
                {
                    return authorization.Substring(scheme.Length + 1).Trim();
                }

                return null;
            };
        }

        /// <summary>
        /// Reads the token from a query string parameter.
        /// </summary>
        /// <param name="name">The name (defaults to access_token).</param>
        /// <returns></returns>
        public static Func<HttpRequest, string> FromQueryString(string name = "access_token")
        {
            return (request) => request.Query[name].FirstOrDefault();
        }
    }
}
