using System.Text.Json;

namespace IdentityModel.Client
{
	/// <summary>
	/// A token response that can be instantiated from a cached json document. The original TokenResponse class 
	/// only supported instantiation from a HttpResponseMessage.
	/// </summary>
	public class CachedTokenResponse : TokenResponse
	{
		public CachedTokenResponse(string cachedJson)
		{
			Raw = cachedJson;
			Json = JsonDocument.Parse(cachedJson).RootElement;
		}
	}
}
