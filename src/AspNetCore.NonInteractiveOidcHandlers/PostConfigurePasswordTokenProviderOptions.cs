using System.Collections.Concurrent;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace AspNetCore.NonInteractiveOidcHandlers
{
    public class PostConfigurePasswordTokenProviderOptions: IPostConfigureOptions<PasswordTokenProviderOptions>
    {
        public void PostConfigure(string name, PasswordTokenProviderOptions options)
        {
            options.LazyTokens = new ConcurrentDictionary<string, AsyncLazy<TokenResponse>>();
        }
    }
}
