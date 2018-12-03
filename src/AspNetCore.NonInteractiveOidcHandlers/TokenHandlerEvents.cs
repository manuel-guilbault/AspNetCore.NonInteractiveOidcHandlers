using System;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace AspNetCore.NonInteractiveOidcHandlers
{
    public class TokenHandlerEvents
    {
        public Func<TokenResponse, Task> OnTokenAcquired { get; set; } = tokenResponse => Task.CompletedTask;

        public Func<TokenResponse, Task> OnTokenRequestFailed { get; set; } = tokenResponse => Task.CompletedTask;

        public virtual Task TokenAcquired(TokenResponse tokenResponse) => OnTokenAcquired?.Invoke(tokenResponse) ?? Task.CompletedTask;

        public virtual Task TokenRequestFailed(TokenResponse tokenResponse) => OnTokenRequestFailed?.Invoke(tokenResponse) ?? Task.CompletedTask;
    }
}
