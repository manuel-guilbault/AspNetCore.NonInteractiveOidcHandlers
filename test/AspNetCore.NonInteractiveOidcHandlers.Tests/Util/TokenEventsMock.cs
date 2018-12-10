using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace AspNetCore.NonInteractiveOidcHandlers.Tests.Util
{
	public class TokenEventsMock
	{
		public TokenResponse LatestTokenAcquired { get; private set; }
		public TokenResponse LatestTokenRequestFailed { get; private set; }

		public TokenHandlerEvents CreateEvents()
			=> new TokenHandlerEvents()
			{
				OnTokenAcquired = tokenResponse =>
				{
					LatestTokenAcquired = tokenResponse;
					return Task.CompletedTask;
				},
				OnTokenRequestFailed = tokenResponse =>
				{
					LatestTokenRequestFailed = tokenResponse;
					return Task.CompletedTask;
				},
			};
	}
}
