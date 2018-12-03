using System;
using System.Net.Http;
using System.Threading.Tasks;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	internal static class TokenHandlerOptionsExtensions
	{
		public static async Task<string> GetTokenEndpointAsync(this TokenHandlerOptions options, HttpClient authorityHttpClient)
		{
			if (options.TokenEndpoint.IsPresent())
			{
				return options.TokenEndpoint;
			}

			var endpoint = await options.GetTokenEndpointFromDiscoveryDocument(authorityHttpClient).ConfigureAwait(false);
			return endpoint;
		}

		public static async Task<string> GetTokenEndpointFromDiscoveryDocument(this TokenHandlerOptions options, HttpClient authorityHttpClient)
		{
			var discoveryRequest = new DiscoveryDocumentRequest
			{
				Address = options.Authority,
				Policy = options.DiscoveryPolicy,
			};
			var discoveryResponse = await authorityHttpClient.GetDiscoveryDocumentAsync(discoveryRequest).ConfigureAwait(false);
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
