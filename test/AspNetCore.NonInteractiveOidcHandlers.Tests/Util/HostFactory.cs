using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.NonInteractiveOidcHandlers.Tests.Util
{
	static class HostFactory
	{
		private const string DefaultApi = "default";

		public static IServiceProvider CreateHost(
			Action<IHttpClientBuilder> addTokenHandler,
			TokenEndpointHandler tokenEndpointHandler = null,
			bool addCaching = false,
			DownstreamApiHandler api = null)
			=> CreateHost(
				tokenEndpointHandler,
				addCaching,
				new DownstreamApi(DefaultApi, api ?? new DownstreamApiHandler(), addTokenHandler));

		public static IServiceProvider CreateHost(
			TokenEndpointHandler tokenEndpointHandler = null,
			bool addCaching = false,
			params DownstreamApi[] apis)
		{
			return CreateHost(
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
				apis);
		}

		public static IServiceProvider CreateHost(
			Action<IServiceCollection> configureServices = null,
			bool addCaching = false,
			params DownstreamApi[] apis)
		{
			var services = new ServiceCollection();

			configureServices?.Invoke(services);

			if (addCaching)
			{
				services.AddDistributedMemoryCache();
			}

			foreach (var api in apis)
			{
				var httpClientBuilder = services.AddHttpClient(api.Name, o => { o.BaseAddress = new Uri($"https://{api.Name}"); });
				api.AddTokenHandler(httpClientBuilder);
				httpClientBuilder.AddHttpMessageHandler(_ => api.Handler);
			}

			return services.BuildServiceProvider();
		}

		public static HttpClient GetHttpClient(this IServiceProvider serviceProvider)
			=> serviceProvider.GetHttpClient(DefaultApi);

		public static HttpClient GetHttpClient(this IServiceProvider serviceProvider, string apiName)
			=> serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(apiName);

		public static HttpClient CreateClient(
			Action<IHttpClientBuilder> addTokenHandler,
			TokenEndpointHandler tokenEndpointHandler = null,
			bool addCaching = false,
			DownstreamApiHandler api = null)
			=> CreateHost(addTokenHandler, tokenEndpointHandler, addCaching, api).GetHttpClient();

		public static HttpClient CreateClient(
			string apiName,
			Action<IServiceCollection> configureServices = null,
			bool addCaching = false,
			params DownstreamApi[] apis)
			=> CreateHost(configureServices, addCaching, apis).GetHttpClient(apiName);
	}
}
