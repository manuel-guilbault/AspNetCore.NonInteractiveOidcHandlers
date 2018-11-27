using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AspNetCore.NonInteractiveOidcHandlers.Tests.Util
{
	class DiscoveryEndpointHandler : HttpMessageHandler
	{
		public string Endpoint { get; set; }

		public bool IsFailureTest { get; set; } = false;

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
		{
			if (IsFailureTest)
			{
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
			}

			if (request.RequestUri.AbsoluteUri == "https://authority.com/.well-known/openid-configuration")
			{
				Endpoint = request.RequestUri.AbsoluteUri;

				var data = new Dictionary<string, object>
				{
					{ "issuer", "https://authority.com" },
					{ "token_endpoint", "https://authority.com/token_endpoint" }
				};

				var json = JsonConvert.SerializeObject(data);

				var response = new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(json, Encoding.UTF8, "application/json")
				};

				return Task.FromResult(response);
			}

			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
		}
	}
}
