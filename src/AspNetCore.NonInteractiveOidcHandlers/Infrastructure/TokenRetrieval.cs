using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.NonInteractiveOidcHandlers.Infrastructure
{
	/// <summary>
	/// Defines some common token retrieval strategies
	/// </summary>
	public static class TokenRetrieval
	{
		public static Func<HttpContext, Task<string>> FromAuthenticationService(string tokenName = "access_token")
		{
			return async (context) =>
			{
				var token = await context.GetTokenAsync(tokenName);
				return token;
			};
		}

		/// <summary>
		/// Reads the token from the authrorization header.
		/// </summary>
		/// <param name="scheme">The scheme (defaults to Bearer).</param>
		/// <returns></returns>
		public static Func<HttpContext, Task<string>> FromAuthorizationHeader(string scheme = "Bearer")
		{
			return (context) =>
			{
				var authorization = context.Request.Headers["Authorization"].FirstOrDefault();
				if (string.IsNullOrEmpty(authorization))
				{
					return Task.FromResult<string>(null);
				}

				if (!authorization.StartsWith(scheme + " ", StringComparison.OrdinalIgnoreCase))
				{
					return Task.FromResult<string>(null);
				}

				var token = authorization.Substring(scheme.Length + 1).Trim();
				return Task.FromResult(token);
			};
		}

		/// <summary>
		/// Reads the token from a query string parameter.
		/// </summary>
		/// <param name="name">The name (defaults to access_token).</param>
		/// <returns></returns>
		public static Func<HttpContext, Task<string>> FromQueryString(string name = "access_token")
		{
			return (context) => Task.FromResult(context.Request.Query[name].FirstOrDefault());
		}
	}
}
