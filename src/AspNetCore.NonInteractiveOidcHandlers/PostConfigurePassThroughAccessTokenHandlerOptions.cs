using Microsoft.Extensions.Options;

namespace AspNetCore.NonInteractiveOidcHandlers
{
	public class PostConfigurePassThroughAccessTokenHandlerOptions: IPostConfigureOptions<PassThroughTokenHandlerOptions>
	{
		public void PostConfigure(string name, PassThroughTokenHandlerOptions options)
		{
			options.Validate();
		}
	}
}
