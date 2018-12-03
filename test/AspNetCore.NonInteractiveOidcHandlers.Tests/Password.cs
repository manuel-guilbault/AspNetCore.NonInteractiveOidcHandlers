using System;
using System.Threading.Tasks;
using AspNetCore.NonInteractiveOidcHandlers.Tests.Util;
using Microsoft.Extensions.DependencyInjection;
using NFluent;
using Xunit;

namespace AspNetCore.NonInteractiveOidcHandlers.Tests
{
	public class PasswordTests
	{
		private const string ClientId = "client-id";
		private const string ClientSecret = "client-secret";
		private const string Scope = "downstream-api";
		private const string UserName = "user-name";
		private const string Password = "password";

		private readonly Action<PasswordTokenHandlerOptions> _options = (o) =>
		{
			o.TokenEndpoint = "https://authority/connect/token";
			o.ClientId = ClientId;
			o.ClientSecret = ClientSecret;
			o.Scope = Scope;
			o.UserCredentialsRetriever = (_) => (UserName, Password);
		};

		[Fact]
		public void Token_request_error_should_throw()
		{
			var client = HostFactory
				.CreateClient(
					b => b.AddOidcPassword(_options),
					TokenEndpointHandler.BadRequest("invalid_grant"));

			async Task Act() => await client.GetAsync("https://default");

			Check.ThatAsyncCode(Act)
				.Throws<InvalidOperationException>().WithMessage("Token retrieval failed: invalid_grant ");
		}

		[Fact]
		public async Task Successful_token_request_should_trigger_authenticated_request_to_api()
		{
			var tokenEndpoint = TokenEndpointHandler.ValidBearerToken("access-token", TimeSpan.MaxValue);
			var api = new DownstreamApiHandler();
			var client = HostFactory
				.CreateClient(
					b => b.AddOidcPassword(_options),
					tokenEndpoint,
					api: api);

			await client.GetAsync("https://default");

			Check.That(tokenEndpoint.LastRequestClientId).IsEqualTo(ClientId);
			Check.That(tokenEndpoint.LastRequestClientSecret).IsEqualTo(ClientSecret);
			Check.That(tokenEndpoint.LastRequestScope).IsEqualTo(Scope);
			Check.That(tokenEndpoint.LastRequestUserName).IsEqualTo(UserName);
			Check.That(tokenEndpoint.LastRequestPassword).IsEqualTo(Password);
			Check.That(api.LastRequestToken).IsEqualTo("access-token");
		}

		[Fact]
		public async Task Successful_token_request_should_trigger_authenticated_request_to_proper_api_when_multiple_clients_registered()
		{
			var tokenEndpointA = TokenEndpointHandler.ValidBearerToken("api-token-a", TimeSpan.MaxValue);
			var apiA = new DownstreamApiHandler();
			var tokenEndpointB = TokenEndpointHandler.ValidBearerToken("api-token-b", TimeSpan.MaxValue);
			var apiB = new DownstreamApiHandler();
			var client = HostFactory
				.CreateClient(
					"api-b",
					services =>
					{
						services.AddHttpClient("api-a-authority").AddHttpMessageHandler(() => tokenEndpointA);
						services.AddHttpClient("api-b-authority").AddHttpMessageHandler(() => tokenEndpointB);
					},
					false,
					new DownstreamApi("api-a", apiA, b => b.AddOidcPassword(o =>
					{
						_options(o);
						o.Scope = "api-a";
						o.AuthorityHttpClientName = "api-a-authority";
					})),
					new DownstreamApi("api-b", apiB, b => b.AddOidcPassword(o =>
					{
						_options(o);
						o.Scope = "api-b";
						o.AuthorityHttpClientName = "api-b-authority";
					})));

			await client.GetAsync("https://api-b");

			Check.That(apiA.LastRequestToken).IsNull();
			Check.That(apiB.LastRequestToken).IsEqualTo("api-token-b");
		}
	}
}
