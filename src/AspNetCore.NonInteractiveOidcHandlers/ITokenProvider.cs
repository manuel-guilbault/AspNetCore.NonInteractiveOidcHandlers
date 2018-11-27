using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace AspNetCore.NonInteractiveOidcHandlers
{
    public interface ITokenProvider
    {
        Task<TokenResponse> GetTokenAsync(CancellationToken cancellationToken);
    }
}
