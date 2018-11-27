using System;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	public class ClientCredentialsTokenProvider: CachingTokenProvider, ITokenProvider
	{
		private readonly ILogger<ClientCredentialsTokenProvider> _logger;
		private readonly ClientCredentialsTokenProviderOptions _options;

		public ClientCredentialsTokenProvider(
			ILogger<ClientCredentialsTokenProvider> logger,
			IDistributedCache cache,
			ClientCredentialsTokenProviderOptions options)
			: base(logger, cache, options)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_options = options ?? throw new ArgumentNullException(nameof(options));
		}
		
		public override async Task<TokenResponse> GetTokenAsync(CancellationToken cancellationToken)
		{
			return await GetTokenAsync("client_credentials", AcquireTokenAsync, cancellationToken).ConfigureAwait(false);
		}

		private async Task<TokenResponse> AcquireTokenAsync(CancellationToken cancellationToken)
		{
			var tokenResponse = await _options.LazyToken.Value.ConfigureAwait(false);
			if (tokenResponse.IsError)
			{
				_logger.LogError($"Error returned from token endpoint: {tokenResponse.Error}");
				await _options.Events.OnTokenRequestFailed.Invoke(tokenResponse).ConfigureAwait(false);
				throw new InvalidOperationException(
					$"Token retrieval failed: {tokenResponse.Error} {tokenResponse.ErrorDescription}",
					tokenResponse.Exception);
			}

			await _options.Events.OnTokenAcquired(tokenResponse).ConfigureAwait(false);
			return tokenResponse;
		}
	}
}
