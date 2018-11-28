using System;
using AspNetCore.NonInteractiveOidcHandlers;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
	public static class HttpClientBuilderExtensions
	{
		public static IHttpClientBuilder AddOidcTokenDelegation(this IHttpClientBuilder builder, Action<DelegationTokenProviderOptions> configureOptions)
		{
			if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

			builder.Services
				.AddHttpContextAccessor()
				.Configure(builder.Name, configureOptions)
				.AddPostConfigure<DelegationTokenProviderOptions, PostConfigureTokenProviderOptions<DelegationTokenProviderOptions>>()
				.AddPostConfigure<DelegationTokenProviderOptions, PostConfigureDelegationTokenProviderOptions>();
			
			var instanceName = builder.Name;
			return builder.AddHttpMessageHandler(sp =>
			{
				var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<DelegationTokenProviderOptions>>();
				var options = optionsMonitor.Get(instanceName);
				
				return new BearerTokenHandler(
					new DelegationTokenProvider(
						sp.GetRequiredService<ILogger<DelegationTokenProvider>>(),
						sp.GetRequiredService<IHttpContextAccessor>(),
						sp.GetService<IDistributedCache>(),
						options));
			});
		}

		public static IHttpClientBuilder AddOidcClientCredentials(this IHttpClientBuilder builder, Action<ClientCredentialsTokenProviderOptions> configureOptions)
		{
			if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

			builder.Services
				.Configure(builder.Name, configureOptions)
				.AddPostConfigure<ClientCredentialsTokenProviderOptions, PostConfigureTokenProviderOptions<ClientCredentialsTokenProviderOptions>>()
				.AddPostConfigure<ClientCredentialsTokenProviderOptions, PostConfigureClientCredentialsTokenProviderOptions>();

			var instanceName = builder.Name;
			return builder.AddHttpMessageHandler(sp =>
			{
				var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<ClientCredentialsTokenProviderOptions>>();
				var options = optionsMonitor.Get(instanceName);

				return new BearerTokenHandler(
					new ClientCredentialsTokenProvider(
						sp.GetRequiredService<ILogger<ClientCredentialsTokenProvider>>(),
						sp.GetService<IDistributedCache>(),
						options));
			});
		}

		public static IHttpClientBuilder AddOidcPassword(this IHttpClientBuilder builder, Action<PasswordTokenProviderOptions> configureOptions)
		{
			if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

			builder.Services
				.Configure(builder.Name, configureOptions)
				.AddPostConfigure<PasswordTokenProviderOptions, PostConfigureTokenProviderOptions<PasswordTokenProviderOptions>>()
				.AddPostConfigure<PasswordTokenProviderOptions, PostConfigurePasswordTokenProviderOptions>();

			var instanceName = builder.Name;
			return builder.AddHttpMessageHandler(sp =>
			{
				var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<PasswordTokenProviderOptions>>();
				var options = optionsMonitor.Get(instanceName);

				return new BearerTokenHandler(
					new PasswordTokenProvider(
						sp.GetRequiredService<ILogger<PasswordTokenProvider>>(),
						sp.GetService<IDistributedCache>(),
						options,
						sp));
			});
		}

		public static IHttpClientBuilder AddOidcRefreshToken(this IHttpClientBuilder builder, Action<RefreshTokenProviderOptions> configureOptions)
		{
			if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

			builder.Services
				.Configure(builder.Name, configureOptions)
				.AddPostConfigure<RefreshTokenProviderOptions, PostConfigureTokenProviderOptions<RefreshTokenProviderOptions>>()
				.AddPostConfigure<RefreshTokenProviderOptions, PostConfigureRefreshTokenProviderOptions>();

			var instanceName = builder.Name;
			return builder.AddHttpMessageHandler(sp =>
			{
				var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<RefreshTokenProviderOptions>>();
				var options = optionsMonitor.Get(instanceName);

				return new BearerTokenHandler(
					new RefreshTokenProvider(
						sp.GetRequiredService<ILogger<RefreshTokenProvider>>(),
						sp.GetService<IDistributedCache>(),
						options,
						sp));
			});
		}
	}
}
