using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.NonInteractiveOidcHandlers
{
    public class BearerTokenHandler: DelegatingHandler
    {
        private readonly ITokenProvider _tokenProvider;

        public BearerTokenHandler(ITokenProvider tokenProvider)
        {
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            if (token != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
