using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using IdentityModel.Client;
using AspNetCore.OAuth2TokenDelegation.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace AspNetCore.OAuth2TokenDelegation
{
    internal class PostConfigureOAuth2TokenDelegationOptions: IPostConfigureOptions<OAuth2TokenDelegationOptions>
    {
        private readonly IDistributedCache _cache;

        public PostConfigureOAuth2TokenDelegationOptions(IDistributedCache cache = null)
        {
            _cache = cache;
        }

        public void PostConfigure(string name, OAuth2TokenDelegationOptions options)
        {
            options.Validate();

            if (options.EnableCaching && _cache == null)
            {
                throw new InvalidOperationException("Caching is enabled, but no IDistributedCache is found in the services collection.");
            }

            options.TokenClient = new AsyncLazy<TokenClient>(() => InitializeTokenClient(options));
            options.LazyTokens = new ConcurrentDictionary<string, AsyncLazy<TokenResponse>>();
        }

        private async Task<string> GetTokenEndpointFromDiscoveryDocument(OAuth2TokenDelegationOptions options)
        {
            var client = options.DiscoveryHttpHandler != null
                ? new DiscoveryClient(options.Authority, options.DiscoveryHttpHandler)
                : new DiscoveryClient(options.Authority);

            client.Timeout = options.DiscoveryTimeout;
            client.Policy = options?.DiscoveryPolicy ?? new DiscoveryPolicy();

            var disco = await client.GetAsync().ConfigureAwait(false);
            if (disco.IsError)
            {
                if (disco.ErrorType == ResponseErrorType.Http)
                {
                    throw new InvalidOperationException($"Discovery endpoint {client.Url} is unavailable: {disco.Error}");
                }
                if (disco.ErrorType == ResponseErrorType.PolicyViolation)
                {
                    throw new InvalidOperationException($"Policy error while contacting the discovery endpoint {client.Url}: {disco.Error}");
                }
                if (disco.ErrorType == ResponseErrorType.Exception)
                {
                    throw new InvalidOperationException($"Error parsing discovery document from {client.Url}: {disco.Error}");
                }
            }

            return disco.TokenEndpoint;
        }

        private async Task<string> GetTokenEndpoint(OAuth2TokenDelegationOptions options)
        {
            if (options.TokenEndpoint.IsPresent())
            {
                return options.TokenEndpoint;
            }

            var endpoint = await GetTokenEndpointFromDiscoveryDocument(options).ConfigureAwait(false);
            options.TokenEndpoint = endpoint;
            return endpoint;
        }

        private async Task<TokenClient> InitializeTokenClient(OAuth2TokenDelegationOptions options)
        {
            var endpoint = await GetTokenEndpoint(options).ConfigureAwait(false);

            var client = options.TokenHttpHandler != null
                ? new TokenClient(endpoint, options.ClientId, options.ClientSecret, options.TokenHttpHandler, options.AuthenticationStyle)
                : new TokenClient(endpoint, options.ClientId, options.ClientSecret, style: options.AuthenticationStyle);

            client.Timeout = options.TokenTimeout;
            return client;
        }
    }
}
