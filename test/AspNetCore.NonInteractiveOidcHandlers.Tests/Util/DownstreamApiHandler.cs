using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.NonInteractiveOidcHandlers.Tests.Util
{
	class DownstreamApiHandler: DelegatingHandler
	{
		public string LastRequestToken { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			LastRequestToken = request.Headers.Authorization?.Parameter;

			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
		}
	}
}
