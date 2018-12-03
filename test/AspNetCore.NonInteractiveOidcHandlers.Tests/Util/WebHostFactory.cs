using System;
using System.Net.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.NonInteractiveOidcHandlers.Tests.Util
{
	class WebHostFactory
	{
		private const string DefaultDownstreamApi = "default";

		public static TestServer CreateServer(
			Action<IHttpClientBuilder> addTokenHandler,
			TokenEndpointHandler tokenEndpointHandler = null,
			bool addCaching = false,
			DownstreamApiHandler downstreamApi = null)
			=> CreateServer(tokenEndpointHandler, addCaching, new DownstreamApi(DefaultDownstreamApi, downstreamApi ?? new DownstreamApiHandler(), addTokenHandler));

		public static TestServer CreateServer(
			TokenEndpointHandler tokenEndpointHandler = null,
			bool addCaching = false,
			params DownstreamApi[] downstreamApis)
			=> CreateServer(
				services =>
				{
					if (tokenEndpointHandler != null)
					{
						services
							.AddHttpClient(TokenHandlerOptions.DefaultAuthorityHttpClientName)
							.AddHttpMessageHandler(() => tokenEndpointHandler);
					}
				},
				addCaching,
				downstreamApis);

		public static TestServer CreateServer(
			Action<IServiceCollection> configureServices = null,
			bool addCaching = false,
			params DownstreamApi[] downstreamApis)
		{
			return new TestServer(new WebHostBuilder()
				.ConfigureServices(services =>
				{
					configureServices?.Invoke(services);

					if (addCaching)
					{
						services.AddDistributedMemoryCache();
					}

					services.AddTransient<IAuthenticationService, AuthenticationServiceMock>();

					foreach (var downstreamApi in downstreamApis)
					{
						var httpClientBuilder = services.AddHttpClient(downstreamApi.Name, o => { o.BaseAddress = new Uri($"https://{downstreamApi.Name}"); });
						downstreamApi.AddTokenHandler(httpClientBuilder);
						httpClientBuilder.AddHttpMessageHandler(_ => downstreamApi.Handler);
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
			Action<IHttpClientBuilder> addTokenHandler,
			TokenEndpointHandler tokenEndpointHandler = null,
			bool addCaching = false,
			DownstreamApiHandler downstreamApi = null)
			=> CreateServer(addTokenHandler, tokenEndpointHandler, addCaching, downstreamApi)
				.CreateClient();

		public static HttpClient CreateClient(
			Action<IServiceCollection> configureServices = null,
			bool addCaching = false,
			params DownstreamApi[] downstreamApis)
			=> CreateServer(configureServices, addCaching, downstreamApis)
				.CreateClient();
	}
}
