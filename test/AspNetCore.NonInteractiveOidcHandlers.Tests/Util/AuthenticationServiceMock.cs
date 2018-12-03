using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.NonInteractiveOidcHandlers.Tests.Util
{
	/// <summary>
	/// An IAuthenticationService mock which authenticates the inbound request with a ticket containing the inbound
	/// Bearer token as access_token. If there is no inbound token, the request is considered anonymous.
	///
	/// This mock allows <code>HttpContext.GetTokenAsync("access_token")</code> to work properly and to return the
	/// inbound Bearer token.
	/// </summary>
	public class AuthenticationServiceMock : IAuthenticationService
	{
		private static readonly Func<HttpContext, Task<string>> GetTokenFromHeader = TokenRetrieval.FromAuthorizationHeader();

		public async Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme)
		{
			var token = await GetTokenFromHeader(context);
			if (token == null)
			{
				return AuthenticateResult.NoResult();
			}

			var principal = new ClaimsPrincipal(new ClaimsIdentity(
				new[]
				{
					new Claim("sub", "a-user"),
				},
				scheme));

			var properties = new AuthenticationProperties();
			properties.StoreTokens(new[] { new AuthenticationToken { Name = "access_token", Value = token } });

			return AuthenticateResult.Success(new AuthenticationTicket(principal, properties, scheme));
		}

		public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties)
		{
			throw new NotImplementedException();
		}

		public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties)
		{
			throw new NotImplementedException();
		}

		public Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties)
		{
			throw new NotImplementedException();
		}

		public Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties)
		{
			throw new NotImplementedException();
		}
	}
}
