using System;
using System.Net.Http;
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
		public static IHttpClientBuilder AddAccessTokenPassThrough(this IHttpClientBuilder builder, Action<PassThroughTokenHandlerOptions> configureOptions)
		{
			if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

			builder.Services.Configure(builder.Name, configureOptions);

			return builder.AddAccessTokenPassThrough();
		}

		public static IHttpClientBuilder AddAccessTokenPassThrough(this IHttpClientBuilder builder)
		{
			builder.Services
				.AddHttpContextAccessor()
				.AddPostConfigure<PassThroughTokenHandlerOptions, PostConfigurePassThroughAccessTokenHandlerOptions>();

			var instanceName = builder.Name;
			return builder.AddHttpMessageHandler(sp =>
			{
				var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<PassThroughTokenHandlerOptions>>();
				var options = optionsMonitor.Get(instanceName);

				return new PassThroughTokenHandler(
					sp.GetRequiredService<IHttpContextAccessor>(),
					options);
			});
		}

		public static IHttpClientBuilder AddOidcTokenDelegation(this IHttpClientBuilder builder, Action<DelegationTokenHandlerOptions> configureOptions)
		{
			if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

			builder.Services
				.AddHttpContextAccessor()
				.Configure(builder.Name, configureOptions)
				.AddPostConfigure<DelegationTokenHandlerOptions, PostConfigureTokenHandlerOptions<DelegationTokenHandlerOptions>>()
			;
			
			var instanceName = builder.Name;
			return builder.AddHttpMessageHandler(sp =>
			{
				var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<DelegationTokenHandlerOptions>>();
				var options = optionsMonitor.Get(instanceName);
				
				return new DelegationTokenHandler(
					sp.GetRequiredService<ILogger<DelegationTokenHandler>>(),
					sp.GetRequiredService<IHttpContextAccessor>(),
					sp.GetRequiredService<IHttpClientFactory>(),
					sp.GetService<IDistributedCache>(),
					options);
			});
		}

		public static IHttpClientBuilder AddOidcClientCredentials(this IHttpClientBuilder builder, Action<ClientCredentialsTokenHandlerOptions> configureOptions)
		{
			if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

			builder.Services
				.Configure(builder.Name, configureOptions)
				.AddPostConfigure<ClientCredentialsTokenHandlerOptions, PostConfigureTokenHandlerOptions<ClientCredentialsTokenHandlerOptions>>()
			;

			var instanceName = builder.Name;
			return builder.AddHttpMessageHandler(sp =>
			{
				var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<ClientCredentialsTokenHandlerOptions>>();
				var options = optionsMonitor.Get(instanceName);

				return new ClientCredentialsTokenHandler(
					sp.GetRequiredService<ILogger<ClientCredentialsTokenHandler>>(),
					sp.GetRequiredService<IHttpClientFactory>(),
					sp.GetService<IDistributedCache>(),
					options);
			});
		}

		public static IHttpClientBuilder AddOidcPassword(this IHttpClientBuilder builder, Action<PasswordTokenHandlerOptions> configureOptions)
		{
			if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

			builder.Services
				.Configure(builder.Name, configureOptions)
				.AddPostConfigure<PasswordTokenHandlerOptions, PostConfigureTokenHandlerOptions<PasswordTokenHandlerOptions>>()
			;

			var instanceName = builder.Name;
			return builder.AddHttpMessageHandler(sp =>
			{
				var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<PasswordTokenHandlerOptions>>();
				var options = optionsMonitor.Get(instanceName);

				return new PasswordTokenHandler(
					sp.GetRequiredService<ILogger<PasswordTokenHandler>>(),
					sp.GetRequiredService<IHttpClientFactory>(),
					sp.GetService<IDistributedCache>(),
					options,
					sp);
			});
		}

		public static IHttpClientBuilder AddOidcRefreshToken(this IHttpClientBuilder builder, Action<RefreshTokenHandlerOptions> configureOptions)
		{
			if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

			builder.Services
				.Configure(builder.Name, configureOptions)
				.AddPostConfigure<RefreshTokenHandlerOptions, PostConfigureTokenHandlerOptions<RefreshTokenHandlerOptions>>()
			;

			var instanceName = builder.Name;
			return builder.AddHttpMessageHandler(sp =>
			{
				var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<RefreshTokenHandlerOptions>>();
				var options = optionsMonitor.Get(instanceName);

				return new RefreshTokenHandler(
					sp.GetRequiredService<ILogger<RefreshTokenHandler>>(),
					sp.GetRequiredService<IHttpClientFactory>(),
					sp.GetService<IDistributedCache>(),
					options,
					sp);
			});
		}
	}
}
