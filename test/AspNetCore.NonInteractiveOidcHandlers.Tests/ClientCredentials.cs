using System;
using System.Threading.Tasks;
using AspNetCore.NonInteractiveOidcHandlers.Tests.Util;
using IdentityModel.Client;
using Microsoft.Extensions.DependencyInjection;
using NFluent;
using Xunit;

namespace AspNetCore.NonInteractiveOidcHandlers.Tests
{
	public class ClientCredentials
	{
		private const string ClientId = "client-id";
		private const string ClientSecret = "client-secret";
		private const string Scope = "downstream-api";

		private readonly Action<ClientCredentialsTokenHandlerOptions> _options = (o) =>
		{
			o.TokenEndpoint = "https://authority/connect/token";
			o.ClientId = ClientId;
			o.ClientSecret = ClientSecret;
			o.Scope = Scope;
		};

		[Fact]
		public async Task Token_request_error_should_trigger_unauthenticated_request_to_api()
		{
			var api = new DownstreamApiHandler();
			var client = HostFactory.CreateClient(
				b => b.AddOidcClientCredentials(_options),
				TokenEndpointHandler.OidcProtocolError("invalid_grant"),
				api: api);

			await client.GetAsync("https://default");

			Check.That(api.LastRequestToken).IsNull();
		}

		[Fact]
		public async Task Token_request_error_should_trigger_TokenRequestFailed_event()
		{
			var eventsMock = new TokenEventsMock();
			var client = HostFactory.CreateClient(
				b => b.AddOidcClientCredentials(o =>
				{
					_options(o);
					o.Events = eventsMock.CreateEvents();
				}),
				TokenEndpointHandler.OidcProtocolError("invalid_grant"));

			await client.GetAsync("https://default");

			Check.That(eventsMock.LatestTokenRequestFailed).IsNotNull();
			Check.That(eventsMock.LatestTokenRequestFailed.ErrorType).IsEqualTo(ResponseErrorType.Protocol);
			Check.That(eventsMock.LatestTokenRequestFailed.Error).IsEqualTo("invalid_grant");
		}

		[Fact]
		public async Task Successful_token_request_should_trigger_authenticated_request_to_api()
		{
			var tokenEndpoint = TokenEndpointHandler.ValidBearerToken("access-token", TimeSpan.MaxValue);
			var api = new DownstreamApiHandler();
			var client = HostFactory.CreateClient(
				b => b.AddOidcClientCredentials(_options),
				tokenEndpoint,
				api: api);

			await client.GetAsync("https://default");

			Check.That(tokenEndpoint.LastRequestClientId).IsEqualTo(ClientId);
			Check.That(tokenEndpoint.LastRequestClientSecret).IsEqualTo(ClientSecret);
			Check.That(tokenEndpoint.LastRequestScope).IsEqualTo(Scope);
			Check.That(api.LastRequestToken).IsEqualTo("access-token");
		}

		[Fact]
		public async Task Successful_token_request_should_trigger_TokenAcquired_event()
		{
			var eventsMock = new TokenEventsMock();
			var client = HostFactory.CreateClient(
				b => b.AddOidcClientCredentials(o =>
				{
					_options(o);
					o.Events = eventsMock.CreateEvents();
				}),
				TokenEndpointHandler.ValidBearerToken("access-token", TimeSpan.MaxValue));

			await client.GetAsync("https://default");

			Check.That(eventsMock.LatestTokenAcquired).IsNotNull();
			Check.That(eventsMock.LatestTokenAcquired.AccessToken).IsEqualTo("access-token");
		}

		[Fact]
		public async Task Successful_token_request_should_trigger_authenticated_request_to_proper_api_when_multiple_clients_registered()
		{
			var tokenEndpointA = TokenEndpointHandler.ValidBearerToken("api-token-a", TimeSpan.MaxValue);
			var apiA = new DownstreamApiHandler();
			var tokenEndpointB = TokenEndpointHandler.ValidBearerToken("api-token-b", TimeSpan.MaxValue);
			var apiB = new DownstreamApiHandler();
			var client = HostFactory.CreateClient(
				"api-b",
				services =>
				{
					services.AddHttpClient("api-a-authority").AddHttpMessageHandler(() => tokenEndpointA);
					services.AddHttpClient("api-b-authority").AddHttpMessageHandler(() => tokenEndpointB);
				},
				false,
				new DownstreamApi("api-a", apiA, b => b.AddOidcClientCredentials(o =>
				{
					_options(o);
					o.Scope = "api-a";
					o.AuthorityHttpClientName = "api-a-authority";
				})),
				new DownstreamApi("api-b", apiB, b => b.AddOidcClientCredentials(o =>
				{
					_options(o);
					o.Scope = "downstream-api-b";
					o.AuthorityHttpClientName = "api-b-authority";
				})));

			await client.GetAsync("https://api-b");

			Check.That(apiA.LastRequestToken).IsNull();
			Check.That(apiB.LastRequestToken).IsEqualTo("api-token-b");
		}
	}
}
