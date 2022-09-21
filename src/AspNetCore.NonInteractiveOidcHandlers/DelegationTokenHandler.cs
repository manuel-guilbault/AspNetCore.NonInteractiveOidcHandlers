using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	public class DelegationTokenHandler: CachingTokenHandler
	{
		private readonly ILogger<DelegationTokenHandler> _logger;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly DelegationTokenHandlerOptions _options;

		public DelegationTokenHandler(
			ILogger<DelegationTokenHandler> logger,
			IHttpContextAccessor httpContextAccessor,
			IHttpClientFactory httpClientFactory,
			IDistributedCache cache,
			DelegationTokenHandlerOptions options)
			: base(logger, cache, options)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
			_httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
			_options = options ?? throw new ArgumentNullException(nameof(options));
		}
		
		public override async Task<TokenResponse> GetTokenAsync(CancellationToken cancellationToken)
		{
			var httpContext = _httpContextAccessor.HttpContext;
			if (httpContext == null)
			{
				_logger.LogTrace("No current HttpContext.");
				return null;
			}

			var inboundToken = await _options.TokenRetriever(httpContext);
			if (inboundToken.IsMissing())
			{
				_logger.LogInformation("No access token in current request.");
				return null;
			}

			return await GetTokenAsync($"delegation:{inboundToken}", _ => AcquireTokenAsync(inboundToken), cancellationToken)
				.ConfigureAwait(false);
		}

		private async Task<TokenResponse> AcquireTokenAsync(string inboundToken)
		{
			var lazyToken = _options.LazyTokens.GetOrAdd(inboundToken, CreateLazyDelegatedToken);

			try
			{
				var tokenResponse = await lazyToken.Value.ConfigureAwait(false);
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
				// If caching is on and it succeeded, the delegated token will be cached.
				// If caching is off and it succeeded, the delegated token will be discarded after this HTTP request.
				// Either way, we want to remove the temporary store of delegated token for this token because it is only intended for de-duping fetch requests
				_options.LazyTokens.TryRemove(inboundToken, out _);
			}
		}
		
		private AsyncLazy<TokenResponse> CreateLazyDelegatedToken(string inboundToken)
			=> new AsyncLazy<TokenResponse>(() => RequestTokenAsync(inboundToken));

		private async Task<TokenResponse> RequestTokenAsync(string inboundToken)
		{
			var httpClient = _httpClientFactory.CreateClient(_options.AuthorityHttpClientName);
			var tokenEndpoint = await _options.GetTokenEndpointAsync(httpClient).ConfigureAwait(false);

			var extraParameters = _options.ExtraTokenParameters?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<string, string>();
			extraParameters["token"] = inboundToken;
			extraParameters["scope"] = _options.Scope;

			var tokenRequest = new TokenRequest
			{
				Address = tokenEndpoint,
				GrantType = _options.GrantType,
				ClientId = _options.ClientId,
				ClientSecret = _options.ClientSecret,
				Parameters = Parameters.FromObject(extraParameters)
			};
			var tokenResponse = await httpClient.RequestTokenAsync(tokenRequest).ConfigureAwait(false);
			return tokenResponse;
		}
	}
}
