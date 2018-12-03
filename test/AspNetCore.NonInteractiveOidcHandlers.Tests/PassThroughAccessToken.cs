using System.Net.Http;
using System.Threading.Tasks;
using AspNetCore.NonInteractiveOidcHandlers.Tests.Util;
using Microsoft.Extensions.DependencyInjection;
using NFluent;
using Xunit;

namespace AspNetCore.NonInteractiveOidcHandlers.Tests
{
	public class PassThroughAccessToken
	{
		[Fact]
		public async Task Unauthenticated_request_from_upstream_should_not_be_passed_through()
		{
			var downstreamApi = new DownstreamApiHandler();
			var client = WebHostFactory.CreateClient(
				b => b.AddAccessTokenPassThrough(),
				downstreamApi: downstreamApi);

			await client.GetAsync("https://default");

			Check.That(downstreamApi.LastRequestToken).IsNull();
		}

		[Fact]
		public async Task Authenticated_request_from_upstream_should_be_passed_through()
		{
			const string accessToken = "1234";

			var downstreamApi = new DownstreamApiHandler();
			var client = WebHostFactory.CreateClient(
				b => b.AddAccessTokenPassThrough(),
				downstreamApi: downstreamApi);
			client.SetBearerToken(accessToken);

			await client.GetAsync("https://default");

			Check.That(downstreamApi.LastRequestToken).IsEqualTo(accessToken);
		}
	}
}
