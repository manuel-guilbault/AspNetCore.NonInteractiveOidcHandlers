using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace AspNetCore.NonInteractiveOidcHandlers
{
    internal class PostConfigureClientCredentialsTokenProviderOptions: IPostConfigureOptions<ClientCredentialsTokenProviderOptions>
    {
        public void PostConfigure(string name, ClientCredentialsTokenProviderOptions options)
        {
            options.LazyToken = new AsyncLazy<TokenResponse>(() => GetToken(options));
        }

        private async Task<TokenResponse> GetToken(ClientCredentialsTokenProviderOptions options)
        {
            var httpClient = options.AuthorityHttpClientAccessor();

            var tokenRequest = new ClientCredentialsTokenRequest
            {
                Address = await options.GetTokenEndpointAsync().ConfigureAwait(false),
                GrantType = options.GrantType,
                ClientId = options.ClientId,
                ClientSecret = options.ClientSecret,
                Scope = options.Scope,
                Parameters = options.ExtraTokenParameters ?? new Dictionary<string, string>(),
            };
            var tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(tokenRequest).ConfigureAwait(false);
            return tokenResponse;
        }
    }
}
