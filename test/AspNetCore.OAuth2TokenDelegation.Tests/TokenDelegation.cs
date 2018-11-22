using System;
using System.Net.Http;
using System.Threading.Tasks;
using AspNetCore.OAuth2TokenDelegation.Tests.Util;
using NFluent;
using Xunit;

namespace AspNetCore.OAuth2TokenDelegation.Tests
{
	public class TokenDelegation
	{
		private readonly Action<OAuth2TokenDelegationOptions> _options = (o) =>
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
			var client = PipelineFactory.CreateClient(_options, downstreamApi: downstreamApi);

			await client.GetAsync("https://default");

			Check.That(downstreamApi.LastRequestToken).IsNull();
		}

		[Fact]
		public void Token_delegation_error_should_throw()
		{
			var client = PipelineFactory.CreateClient(o =>
			{
				_options(o);
				o.TokenHttpHandler = TokenEndpointHandler.BadRequest("invalid_grant");
			});
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
			var client = PipelineFactory.CreateClient(o =>
			{
				_options(o);
				o.TokenHttpHandler = tokenEndpoint;
			}, downstreamApi: downstreamApi);
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
			var client = PipelineFactory.CreateClient(false,
				new DownstreamApi("downstream-a", downstreamApiA, o =>
				{
					_options(o);
					o.Scope = "downstream-api-a";
					o.TokenHttpHandler = tokenEndpointA;
				}),
				new DownstreamApi("downstream-b", downstreamApiB, o =>
				{
					_options(o);
					o.Scope = "downstream-api-b";
					o.TokenHttpHandler = tokenEndpointB;
				}));
			client.SetBearerToken("upstream-token");

			await client.GetAsync("https://downstream-b");

			Check.That(downstreamApiA.LastRequestToken).IsNull();
			Check.That(downstreamApiB.LastRequestToken).IsEqualTo("downstream-token-b");
		}
	}
}
