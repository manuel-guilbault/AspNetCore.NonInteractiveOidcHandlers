using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using IdentityModel.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AspNetCore.OAuth2TokenDelegation.Tests.Util
{
	class TokenEndpointHandler: HttpMessageHandler
	{
		public static TokenEndpointHandler ValidBearerToken(string accessToken, TimeSpan expiresIn)
			=> new TokenEndpointHandler(HttpStatusCode.OK, new
			{
				token_type = "Bearer",
				access_token = accessToken,
				expires_in = expiresIn.TotalSeconds,
			});

		public static TokenEndpointHandler BadRequest(string error)
			=> new TokenEndpointHandler(HttpStatusCode.BadRequest, new { error });

		private readonly HttpStatusCode _statusCode;
		private readonly object _response;

		private TokenEndpointHandler(HttpStatusCode statusCode, object response)
		{
			_statusCode = statusCode;
			_response = response;
		}

		public string LastRequestToken { get; private set; }

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var requestRawPayload = await request.Content.ReadAsStringAsync();
			var requestPayload = HttpUtility.ParseQueryString(requestRawPayload);
			LastRequestToken = requestPayload["token"];

			var json = JsonConvert.SerializeObject(_response);
			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
			return response;
		}
	}
}
