using System.Collections.Concurrent;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace AspNetCore.NonInteractiveOidcHandlers
{
    public class PostConfigureRefreshTokenHandlerOptions: IPostConfigureOptions<RefreshTokenHandlerOptions>
    {
        public void PostConfigure(string name, RefreshTokenHandlerOptions options)
        {
            options.LazyTokens = new ConcurrentDictionary<string, AsyncLazy<TokenResponse>>();
        }
    }
}
