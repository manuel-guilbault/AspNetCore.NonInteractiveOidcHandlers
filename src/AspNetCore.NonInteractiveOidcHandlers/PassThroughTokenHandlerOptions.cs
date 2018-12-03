using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	public class PassThroughTokenHandlerOptions: IValidatableOptions
	{
		/// <summary>
		/// Specifies the method how to retrieve the token from the HTTP request.
		/// </summary>
		public Func<HttpContext, Task<string>> TokenRetriever { get; set; } = TokenRetrieval.FromAuthenticationService();
		
		public IEnumerable<string> GetValidationErrors()
		{
			if (TokenRetriever == null)
			{
				yield return $"You must set {nameof(TokenRetriever)}.";
			}
		}
	}
}
