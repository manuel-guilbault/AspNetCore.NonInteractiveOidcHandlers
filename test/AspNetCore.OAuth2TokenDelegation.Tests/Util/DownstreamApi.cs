using System;

namespace AspNetCore.OAuth2TokenDelegation.Tests.Util
{
	class DownstreamApi
	{
		public DownstreamApi(string name, DownstreamApiHandler handler, Action<OAuth2TokenDelegationOptions> configureOptions)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Handler = handler ?? throw new ArgumentNullException(nameof(handler));
			ConfigureOptions = configureOptions ?? throw new ArgumentNullException(nameof(configureOptions));
		}

		public string Name { get; }
		public DownstreamApiHandler Handler { get; }
		public Action<OAuth2TokenDelegationOptions> ConfigureOptions { get; }
	}
}
