using System;
using System.Threading.Tasks;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;

namespace AspNetCore.NonInteractiveOidcHandlers
{
    internal static class TokenProviderOptionsExtensions
    {
        public static async Task<string> GetTokenEndpointAsync(this TokenProviderOptions options)
        {
            if (options.TokenEndpoint.IsPresent())
            {
                return options.TokenEndpoint;
            }

            var endpoint = await options.GetTokenEndpointFromDiscoveryDocument().ConfigureAwait(false);
            return endpoint;
        }

        public static async Task<string> GetTokenEndpointFromDiscoveryDocument(this TokenProviderOptions options)
        {
            var httpClient = options.AuthorityHttpClientAccessor.Invoke();

            var discoveryRequest = new DiscoveryDocumentRequest
            {
                Address = options.Authority,
                Policy = options.DiscoveryPolicy,
            };
            var discoveryResponse = await httpClient.GetDiscoveryDocumentAsync(discoveryRequest).ConfigureAwait(false);
            if (discoveryResponse.IsError)
            {
                if (discoveryResponse.ErrorType == ResponseErrorType.Http)
                {
                    throw new InvalidOperationException($"Discovery endpoint {discoveryRequest.Address} is unavailable: {discoveryResponse.Error}");
                }
                if (discoveryResponse.ErrorType == ResponseErrorType.PolicyViolation)
                {
                    throw new InvalidOperationException($"Policy error while contacting the discovery endpoint {discoveryRequest.Address}: {discoveryResponse.Error}");
                }
                if (discoveryResponse.ErrorType == ResponseErrorType.Exception)
                {
                    throw new InvalidOperationException($"Error parsing discovery document from {discoveryRequest.Address}: {discoveryResponse.Error}");
                }
            }

            return discoveryResponse.TokenEndpoint;
        }
    }
}
