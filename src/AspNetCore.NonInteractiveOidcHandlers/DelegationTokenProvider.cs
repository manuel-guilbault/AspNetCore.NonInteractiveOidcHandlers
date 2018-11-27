using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	public class DelegationTokenProvider: CachingTokenProvider, ITokenProvider
	{
		private readonly ILogger<DelegationTokenProvider> _logger;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly DelegationTokenProviderOptions _options;

		public DelegationTokenProvider(
			ILogger<DelegationTokenProvider> logger, 
			IHttpContextAccessor httpContextAccessor,
			IDistributedCache cache,
			DelegationTokenProviderOptions options)
			: base(logger, cache, options)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
			_options = options ?? throw new ArgumentNullException(nameof(options));
		}
		
		public override async Task<TokenResponse> GetTokenAsync(CancellationToken cancellationToken)
		{
			var httpContext = _httpContextAccessor.HttpContext;
			if (httpContext == null)
			{
				_logger.LogTrace($"No current HttpContext.");
				return null;
			}

			var inboundToken = _options.TokenRetriever(httpContext.Request);
			if (inboundToken.IsMissing())
			{
				_logger.LogInformation($"No access token in current request.");
				return null;
			}

			return await GetTokenAsync($"delegation:{inboundToken}", ct => AcquireToken(inboundToken, ct), cancellationToken)
				.ConfigureAwait(false);
		}

		private async Task<TokenResponse> AcquireToken(string inboundToken, CancellationToken cancellationToken)
		{
			var lazyToken = _options.LazyTokens.GetOrAdd(inboundToken, CreateLazyDelegatedToken);

			try
			{
				var tokenResponse = await lazyToken.Value.ConfigureAwait(false);
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
			finally
			{
				// If caching is on and it succeeded, the delegated token is now in the cache.
				// If caching is off and it succeeded, the delegated token will be discarded.
				// Either way, we want to remove the temporary store of delegated token for this token because it is only intended for de-duping fetch requests
				_options.LazyTokens.TryRemove(inboundToken, out _);
			}
		}
		
		private AsyncLazy<TokenResponse> CreateLazyDelegatedToken(string inboundToken)
			=> new AsyncLazy<TokenResponse>(() => RequestToken(inboundToken));

		private async Task<TokenResponse> RequestToken(string inboundToken)
		{
			var httpClient = _options.AuthorityHttpClientAccessor();

			var extraParameters = _options.ExtraTokenParameters?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<string, string>();
			extraParameters["token"] = inboundToken;
			extraParameters["scope"] = _options.Scope;

			var tokenRequest = new TokenRequest
			{
				Address = await _options.GetTokenEndpointAsync().ConfigureAwait(false),
				GrantType = _options.GrantType,
				ClientId = _options.ClientId,
				ClientSecret = _options.ClientSecret,
				Parameters = extraParameters,
			};
			var tokenResponse = await httpClient.RequestTokenAsync(tokenRequest).ConfigureAwait(false);
			return tokenResponse;
		}
	}
}
