using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AspNetCore.NonInteractiveOidcHandlers.Infrastructure
{
	internal static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddPostConfigure<TOptions, TPostConfigure>(this IServiceCollection services)
			where TOptions: class
			where TPostConfigure: class, IPostConfigureOptions<TOptions>
		{
			services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<TOptions>, TPostConfigure>());
			return services;
		}
	}
}
