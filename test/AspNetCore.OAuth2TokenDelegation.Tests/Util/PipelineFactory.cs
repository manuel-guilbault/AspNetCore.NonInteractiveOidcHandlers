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
		private const string DefaultDownstreamApi = "default";

		public static TestServer CreateServer(
			Action<OAuth2TokenDelegationOptions> configureOptions,
			bool addCaching = false,
			DownstreamApiHandler downstreamApi = null)
			=> CreateServer(addCaching, new DownstreamApi(DefaultDownstreamApi, downstreamApi ?? new DownstreamApiHandler(), configureOptions));

		public static TestServer CreateServer(
			bool addCaching = false,
			params DownstreamApi[] downstreamApis)
		{
			return new TestServer(new WebHostBuilder()
				.ConfigureServices(services =>
				{
					if (addCaching)
					{
						services.AddDistributedMemoryCache();
					}

					foreach (var downstreamApi in downstreamApis)
					{
						services
							.AddHttpClient(downstreamApi.Name, o => { o.BaseAddress = new Uri($"https://{downstreamApi.Name}"); })
							.AddOAuth2TokenDelegation(downstreamApi.ConfigureOptions)
							.AddHttpMessageHandler(_ => downstreamApi.Handler);
					}
				})
				.Configure(app =>
				{
					app.Use(async (context, next) =>
					{
						var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
						var httpClient = httpClientFactory.CreateClient(context.Request.Host.Host);
						await httpClient.GetAsync(context.Request.Path);

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

		public static HttpClient CreateClient(
			bool addCaching = false,
			params DownstreamApi[] downstreamApis)
			=> CreateServer(addCaching, downstreamApis)
				.CreateClient();
	}
}
