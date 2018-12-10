using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	public class DelegationTokenHandlerOptions: TokenHandlerOptions
	{
		/// <summary>
		/// The grant type used for the token request (default: delegation).
		/// </summary>
		public string GrantType { get; set; } = "delegation";

		/// <summary>
		/// Specifies the scope of the requested tokens (required).
		/// </summary>
		public string Scope { get; set; }

		/// <summary>
		/// Extra parameters passed to the token request.
		/// </summary>
		public IDictionary<string, string> ExtraTokenParameters { get; set; }

		/// <summary>
		/// Specifies the method how to retrieve the token from the HTTP request.
		/// </summary>
		public Func<HttpContext, Task<string>> TokenRetriever { get; set; } = TokenRetrieval.FromAuthenticationService();

		internal ConcurrentDictionary<string, AsyncLazy<TokenResponse>> LazyTokens { get; } = new ConcurrentDictionary<string, AsyncLazy<TokenResponse>>();

		public override IEnumerable<string> GetValidationErrors()
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

			if (TokenRetriever == null)
			{
				yield return $"You must set {nameof(TokenRetriever)}.";
			}
		}
	}
}
