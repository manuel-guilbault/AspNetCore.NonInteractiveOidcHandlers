using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	internal class PostConfigureClientCredentialsTokenProviderOptions: IPostConfigureOptions<ClientCredentialsTokenProviderOptions>
	{
		public void PostConfigure(string name, ClientCredentialsTokenProviderOptions options)
		{
			options.TokenMutex = new AsyncMutex<TokenResponse>();

		}
	}
}
