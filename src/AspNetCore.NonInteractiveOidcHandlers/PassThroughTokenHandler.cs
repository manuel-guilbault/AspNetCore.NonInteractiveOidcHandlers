using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using IdentityModel.Client;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	public class PassThroughTokenHandler : DelegatingHandler
	{
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly PassThroughTokenHandlerOptions _options;

		public PassThroughTokenHandler(
			IHttpContextAccessor httpContextAccessor,
			PassThroughTokenHandlerOptions options)
		{
			_httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
			_options = options ?? throw new ArgumentNullException(nameof(options));
		}

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var httpContext = _httpContextAccessor.HttpContext;
			if (httpContext != null)
			{
				var accessToken = await _options.TokenRetriever(httpContext);
				if (accessToken != null)
				{
					request.SetBearerToken(accessToken);
				}
			}

			return await base.SendAsync(request, cancellationToken);
		}
	}
}
