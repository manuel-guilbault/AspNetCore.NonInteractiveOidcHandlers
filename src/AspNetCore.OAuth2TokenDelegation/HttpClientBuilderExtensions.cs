using System;
using AspNetCore.OAuth2TokenDelegation;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
                .AddTransient<OAuth2TokenDelegationMessageHandler>()
                .Configure(configureOptions)
                .TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<OAuth2TokenDelegationOptions>, PostConfigureOAuth2TokenDelegationOptions>());

            return builder.AddHttpMessageHandler<OAuth2TokenDelegationMessageHandler>();
        }
    }
}
