using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.OAuth2TokenDelegation.Tests.Util
{
	class PipelineFactory
	{
		public static TestServer CreateServer(
			Action<OAuth2TokenDelegationOptions> configureOptions,
			bool addCaching = false,
			DownstreamApiHandler downstreamApi = null)
		{
			const string downstreamApiClientName = "default";

			return new TestServer(new WebHostBuilder()
				.ConfigureServices(services =>
				{
					if (addCaching)
					{
						services.AddDistributedMemoryCache();
					}

					services
						.AddHttpClient(downstreamApiClientName)
						.AddOAuth2TokenDelegation(configureOptions)
						.AddHttpMessageHandler(_ => downstreamApi ?? new DownstreamApiHandler());
				})
				.Configure(app =>
				{
					app.Use(async (context, next) =>
					{
						var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
						var httpClient = httpClientFactory.CreateClient(downstreamApiClientName);
						await httpClient.GetAsync("https://downstream");

						context.Response.StatusCode = 200;
					});
				}));
		}

		public static HttpClient CreateClient(
			Action<OAuth2TokenDelegationOptions> configureOptions,
			bool addCaching = false,
			DownstreamApiHandler downstreamApi = null)
			=> CreateServer(configureOptions, addCaching, downstreamApi)
				.CreateClient();
	}
}
