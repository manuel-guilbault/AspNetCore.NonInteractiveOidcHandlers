using System.Collections.Concurrent;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace AspNetCore.NonInteractiveOidcHandlers
{
    internal class PostConfigureDelegationTokenProviderOptions: IPostConfigureOptions<DelegationTokenProviderOptions>
    {
        public void PostConfigure(string name, DelegationTokenProviderOptions options)
        {
            options.LazyTokens = new ConcurrentDictionary<string, AsyncLazy<TokenResponse>>();
        }
    }
}
