using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.NonInteractiveOidcHandlers.Tests.Util
{
	static class HostFactory
	{
		private const string DefaultApi = "default";

		public static IServiceProvider CreateHost(Action<IHttpClientBuilder> addTokenHandler,
			bool addCaching = false,
			DownstreamApiHandler api = null)
			=> CreateHost(addCaching, new DownstreamApi(DefaultApi, api ?? new DownstreamApiHandler(), addTokenHandler));

		public static IServiceProvider CreateHost(
			bool addCaching = false,
			params DownstreamApi[] apis)
		{
			var services = new ServiceCollection();
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
			bool addCaching = false,
			DownstreamApiHandler api = null)
			=> CreateHost(addTokenHandler, addCaching, api).GetHttpClient();

		public static HttpClient CreateClient(
			string apiName,
			bool addCaching = false,
			params DownstreamApi[] apis)
			=> CreateHost(addCaching, apis).GetHttpClient(apiName);
	}
}
