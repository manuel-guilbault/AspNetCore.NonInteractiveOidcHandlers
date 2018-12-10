using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	public class ClientCredentialsTokenHandler: CachingTokenHandler
	{
		private readonly ILogger<ClientCredentialsTokenHandler> _logger;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ClientCredentialsTokenHandlerOptions _options;

		public ClientCredentialsTokenHandler(
			ILogger<ClientCredentialsTokenHandler> logger,
			IHttpClientFactory httpClientFactory,
			IDistributedCache cache,
			ClientCredentialsTokenHandlerOptions options)
			: base(logger, cache, options)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
			_options = options ?? throw new ArgumentNullException(nameof(options));
		}
		
		public override async Task<TokenResponse> GetTokenAsync(CancellationToken cancellationToken)
		{
			return await GetTokenAsync("client_credentials", AcquireTokenAsync, cancellationToken).ConfigureAwait(false);
		}

		private async Task<TokenResponse> AcquireTokenAsync(CancellationToken cancellationToken)
		{
			var tokenResponseTask = _options.TokenMutex.AcquireAsync(RequestTokenAsync);
			try
			{
				var tokenResponse = await tokenResponseTask.ConfigureAwait(false);
				if (tokenResponse.IsError)
				{
					_logger.LogError($"Error returned from token endpoint: {tokenResponse.Error}");
					await _options.Events.OnTokenRequestFailed.Invoke(tokenResponse).ConfigureAwait(false);
					return tokenResponse;
				}

				await _options.Events.OnTokenAcquired(tokenResponse).ConfigureAwait(false);
				return tokenResponse;
			}
			finally
			{
				// If caching is on and it succeeded, the token will be cached.
				// If caching is off and it succeeded, the token will be discarded after this HTTP request.
				// Either way, we want to remove the temporary store of token because it is only intended for de-duping fetch requests
				_options.TokenMutex.Release();
			}
		}

		private async Task<TokenResponse> RequestTokenAsync()
		{
			var httpClient = _httpClientFactory.CreateClient(_options.AuthorityHttpClientName);
			var tokenEndpoint = await _options.GetTokenEndpointAsync(httpClient).ConfigureAwait(false);
			var tokenRequest = new ClientCredentialsTokenRequest
			{
				Address = tokenEndpoint,
				GrantType = _options.GrantType,
				ClientId = _options.ClientId,
				ClientSecret = _options.ClientSecret,
				Scope = _options.Scope,
				Parameters = _options.ExtraTokenParameters ?? new Dictionary<string, string>(),
			};
			var tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(tokenRequest).ConfigureAwait(false);
			return tokenResponse;
		}
	}
}
