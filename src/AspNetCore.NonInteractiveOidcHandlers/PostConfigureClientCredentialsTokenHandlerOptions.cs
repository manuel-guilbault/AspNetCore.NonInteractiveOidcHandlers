using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	internal class PostConfigureClientCredentialsTokenHandlerOptions: IPostConfigureOptions<ClientCredentialsTokenHandlerOptions>
	{
		public void PostConfigure(string name, ClientCredentialsTokenHandlerOptions options)
		{
			options.TokenMutex = new AsyncMutex<TokenResponse>();

		}
	}
}
