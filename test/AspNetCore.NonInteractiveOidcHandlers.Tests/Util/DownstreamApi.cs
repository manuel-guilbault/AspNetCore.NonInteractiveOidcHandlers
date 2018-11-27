using System;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.NonInteractiveOidcHandlers.Tests.Util
{
	class DownstreamApi
	{
		public DownstreamApi(string name, DownstreamApiHandler handler, Action<IHttpClientBuilder> addTokenHandler)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Handler = handler ?? throw new ArgumentNullException(nameof(handler));
			AddTokenHandler = addTokenHandler ?? throw new ArgumentNullException(nameof(addTokenHandler));
		}

		public string Name { get; }
		public DownstreamApiHandler Handler { get; }
		public Action<IHttpClientBuilder> AddTokenHandler { get; }
	}
}
