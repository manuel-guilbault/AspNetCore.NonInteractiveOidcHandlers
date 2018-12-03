using System.Collections.Concurrent;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace AspNetCore.NonInteractiveOidcHandlers
{
    internal class PostConfigureDelegationTokenHandlerOptions: IPostConfigureOptions<DelegationTokenHandlerOptions>
    {
        public void PostConfigure(string name, DelegationTokenHandlerOptions options)
        {
            options.LazyTokens = new ConcurrentDictionary<string, AsyncLazy<TokenResponse>>();
        }
    }
}
