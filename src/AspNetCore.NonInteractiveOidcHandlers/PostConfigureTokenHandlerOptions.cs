using System;
using System.Net.Http;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	public class PostConfigureTokenHandlerOptions<TOptions>: IPostConfigureOptions<TOptions>
		where TOptions: TokenHandlerOptions
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IDistributedCache _distributedCache;
		private readonly IDiscoveryCache _discoveryCache;

		public PostConfigureTokenHandlerOptions(
			IHttpClientFactory httpClientFactory,
			IDistributedCache distributedCache = null,
			IDiscoveryCache discoveryCache = null)
		{
			_httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
			_distributedCache = distributedCache;
			_discoveryCache = discoveryCache;
		}

		public void PostConfigure(string name, TOptions options)
		{
			options.Validate();

			if (options.EnableCaching && _distributedCache == null)
			{
				throw new InvalidOperationException("Caching is enabled, but no IDistributedCache is found in the services collection.");
			}

			if (options.TokenEndpoint.IsMissing())
			{
				options.DiscoveryCache = _discoveryCache ?? new DiscoveryCache(
					options.Authority,
					() => _httpClientFactory.CreateClient(options.AuthorityHttpClientName),
					options.DiscoveryPolicy);
			}

			options.HttpClientName = name;
		}
	}
}
