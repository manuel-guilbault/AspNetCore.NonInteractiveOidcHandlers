using System.Collections.Concurrent;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace AspNetCore.NonInteractiveOidcHandlers
{
    public class PostConfigurePasswordTokenHandlerOptions: IPostConfigureOptions<PasswordTokenHandlerOptions>
    {
        public void PostConfigure(string name, PasswordTokenHandlerOptions options)
        {
            options.LazyTokens = new ConcurrentDictionary<string, AsyncLazy<TokenResponse>>();
        }
    }
}
