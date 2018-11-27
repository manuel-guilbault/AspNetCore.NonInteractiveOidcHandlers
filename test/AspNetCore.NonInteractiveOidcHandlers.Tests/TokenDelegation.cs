using System;
using System.Net.Http;
using System.Threading.Tasks;
using AspNetCore.NonInteractiveOidcHandlers.Tests.Util;
using Microsoft.Extensions.DependencyInjection;
using NFluent;
using Xunit;

namespace AspNetCore.NonInteractiveOidcHandlers.Tests
{
	public class TokenDelegation
	{
		private readonly Action<DelegationTokenProviderOptions> _options = (o) =>
		{
			o.TokenEndpoint = "https://authority/connect/token";
			o.ClientId = "upstream-api";
			o.ClientSecret = "upstream-api secret";
			o.Scope = "downstream-api";
		};

		[Fact]
		public async Task Unauthenticated_request_from_upstream_should_not_trigger_delegation()
		{
			var downstreamApi = new DownstreamApiHandler();
			var client = WebHostFactory.CreateClient(
				b => b.AddOidcTokenDelegation(_options),
				downstreamApi: downstreamApi);

			await client.GetAsync("https://default");

			Check.That(downstreamApi.LastRequestToken).IsNull();
		}

		[Fact]
		public void Token_delegation_error_should_throw()
		{
			var client = WebHostFactory.CreateClient(
				b => b.AddOidcTokenDelegation(o =>
				{
					_options(o);
					o.AuthorityHttpClientAccessor = () => TokenEndpointHandler.BadRequest("invalid_grant").AsHttpClient();
				}));
			client.SetBearerToken("1234");

			async Task Act() => await client.GetAsync("https://default");

			Check.ThatAsyncCode(Act)
				.Throws<InvalidOperationException>().WithMessage("Token retrieval failed: invalid_grant ");
		}

		[Fact]
		public async Task Authenticated_request_from_upstream_should_trigger_authenticated_request_to_downstream()
		{
			var tokenEndpoint = TokenEndpointHandler.ValidBearerToken("downstream-token", TimeSpan.MaxValue);
			var downstreamApi = new DownstreamApiHandler();
			var client = WebHostFactory.CreateClient(
				b => b.AddOidcTokenDelegation(o =>
				{
					_options(o);
					o.AuthorityHttpClientAccessor = () => tokenEndpoint.AsHttpClient();
				}),
				downstreamApi: downstreamApi);
			client.SetBearerToken("upstream-token");

			await client.GetAsync("https://default");

			Check.That(tokenEndpoint.LastRequestToken).IsEqualTo("upstream-token");
			Check.That(downstreamApi.LastRequestToken).IsEqualTo("downstream-token");
		}

		[Fact]
		public async Task Authenticated_request_from_upstream_should_trigger_authenticated_request_to_proper_downstream_when_multiple_clients_registered()
		{
			var tokenEndpointA = TokenEndpointHandler.ValidBearerToken("downstream-token-a", TimeSpan.MaxValue);
			var downstreamApiA = new DownstreamApiHandler();
			var tokenEndpointB = TokenEndpointHandler.ValidBearerToken("downstream-token-b", TimeSpan.MaxValue);
			var downstreamApiB = new DownstreamApiHandler();
			var client = WebHostFactory.CreateClient(false,
				new DownstreamApi("downstream-a", downstreamApiA, b => b.AddOidcTokenDelegation(o =>
				{
					_options(o);
					o.Scope = "downstream-api-a";
					o.AuthorityHttpClientAccessor = () => tokenEndpointA.AsHttpClient();
				})),
				new DownstreamApi("downstream-b", downstreamApiB, b => b.AddOidcTokenDelegation(o =>
				{
					_options(o);
					o.Scope = "downstream-api-b";
					o.AuthorityHttpClientAccessor = () => tokenEndpointB.AsHttpClient();
				})));
			client.SetBearerToken("upstream-token");

			await client.GetAsync("https://downstream-b");

			Check.That(downstreamApiA.LastRequestToken).IsNull();
			Check.That(downstreamApiB.LastRequestToken).IsEqualTo("downstream-token-b");
		}
	}
}
