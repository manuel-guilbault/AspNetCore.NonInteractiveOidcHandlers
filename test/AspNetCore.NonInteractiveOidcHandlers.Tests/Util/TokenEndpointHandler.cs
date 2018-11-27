using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace AspNetCore.NonInteractiveOidcHandlers.Tests.Util
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

		public NameValueCollection LastRequest { get; private set; }

		public string LastRequestGrantType => LastRequest["grant_type"];
		public string LastRequestClientId => LastRequest["client_id"];
		public string LastRequestClientSecret => LastRequest["client_secret"];
		public string LastRequestScope => LastRequest["scope"];
		public string LastRequestUserName => LastRequest["username"];
		public string LastRequestPassword => LastRequest["password"];
		public string LastRequestRefreshToken => LastRequest["refresh_token"];
		public string LastRequestToken => LastRequest["token"];

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var requestRawPayload = await request.Content.ReadAsStringAsync();
			LastRequest = HttpUtility.ParseQueryString(requestRawPayload);
			
			var json = JsonConvert.SerializeObject(_response);
			var response = new HttpResponseMessage(_statusCode)
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
			return response;
		}

		public HttpClient AsHttpClient() => new HttpClient(this);
	}
}
