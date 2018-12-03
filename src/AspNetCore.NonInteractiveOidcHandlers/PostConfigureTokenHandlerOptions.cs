using System;
using AspNetCore.NonInteractiveOidcHandlers.Infrastructure;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	public class PostConfigureTokenHandlerOptions<TOptions>: IPostConfigureOptions<TOptions>
		where TOptions: TokenHandlerOptions
	{
		private readonly IDistributedCache _cache;

		public PostConfigureTokenHandlerOptions(IDistributedCache cache = null)
		{
			_cache = cache;
		}

		public void PostConfigure(string name, TOptions options)
		{
			options.Validate();

			if (options.EnableCaching && _cache == null)
			{
				throw new InvalidOperationException("Caching is enabled, but no IDistributedCache is found in the services collection.");
			}

			if (options.TokenEndpoint.IsMissing())
			{
				options.DiscoveryCache = new DiscoveryCache(options.Authority, options.AuthorityHttpClientAccessor, options.DiscoveryPolicy);
			}

			options.HttpClientName = name;
		}
	}
}
