using System.Collections.Concurrent;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace AspNetCore.NonInteractiveOidcHandlers
{
    public class PostConfigureRefreshTokenProviderOptions: IPostConfigureOptions<RefreshTokenProviderOptions>
    {
        public void PostConfigure(string name, RefreshTokenProviderOptions options)
        {
            options.LazyTokens = new ConcurrentDictionary<string, AsyncLazy<TokenResponse>>();
        }
    }
}
