using System;
using AspNetCore.OAuth2TokenDelegation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
	public static class HttpClientBuilderExtensions
	{
		public static IHttpClientBuilder AddOAuth2TokenDelegation(this IHttpClientBuilder builder, Action<OAuth2TokenDelegationOptions> configureOptions)
		{
			if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));
			
			builder.Services
				.AddHttpContextAccessor()
				.Configure(builder.Name, configureOptions)
				.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<OAuth2TokenDelegationOptions>, PostConfigureOAuth2TokenDelegationOptions>());

			return builder.AddHttpMessageHandler(s => CreateOAuth2TokenDelegationMessageHandler(
				builder.Name,
				s.GetRequiredService<ILogger<OAuth2TokenDelegationMessageHandler>>(),
				s.GetRequiredService<IHttpContextAccessor>(),
				s.GetRequiredService<IOptionsMonitor<OAuth2TokenDelegationOptions>>(),
				s.GetService<IDistributedCache>()));
		}

		private static OAuth2TokenDelegationMessageHandler CreateOAuth2TokenDelegationMessageHandler(
			string httpClientName,
			ILogger<OAuth2TokenDelegationMessageHandler> logger,
			IHttpContextAccessor httpContextAccessor,
			IOptionsMonitor<OAuth2TokenDelegationOptions> optionsMonitor,
			IDistributedCache cache = null)
		{
			var options = optionsMonitor.Get(httpClientName);
			return new OAuth2TokenDelegationMessageHandler(logger, httpContextAccessor, options, cache);
		}
	}
}
