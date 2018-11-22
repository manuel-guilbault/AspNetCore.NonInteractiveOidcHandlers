using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using IdentityModel.Client;
using AspNetCore.OAuth2TokenDelegation.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.OAuth2TokenDelegation
{
	/// <summary>
	/// Options class for the OAuth 2.0 token delegation HTTP message handler.
	/// </summary>
	public class OAuth2TokenDelegationOptions
	{
		/// <summary>
		/// Sets the base-path of the token provider.
		/// If set, the OpenID Connect discovery document will be used to find the token endpoint.
		/// </summary>
		public string Authority { get; set; }

		/// <summary>
		/// Specifies the id of the token client (required).
		/// </summary>
		public string ClientId { get; set; }

		/// <summary>
		/// Specifies the shared secret of the token client (required).
		/// </summary>
		public string ClientSecret { get; set; }

		/// <summary>
		/// Specifies the scope of the requested tokens (required).
		/// </summary>
		public string Scope { get; set; }

		/// <summary>
		/// The grant type used for the token request (default: delegation).
		/// </summary>
		public string GrantType { get; set; } = "delegation";

		/// <summary>
		/// Extra parameters passed to the token request.
		/// </summary>
		public IDictionary<string, string> ExtraTokenParameters { get; set; }

		/// <summary>
		/// Specifies whether caching is enabled for token delegation responses (requires a distributed cache implementation).
		/// </summary>
		public bool EnableCaching { get; set; } = false;

		/// <summary>
		/// Specifies ttl for token delegation response caches. TimeSpan.MaxValue is used by default, so the delegated token is
		/// cached as long as it is not expired.
		/// </summary>
		public TimeSpan CacheDuration { get; set; } = TimeSpan.MaxValue;

		/// <summary>
		/// Specifies the prefix of the cache key.
		/// </summary>
		public string CacheKeyPrefix { get; set; } = string.Empty;

		/// <summary>
		/// Specifies the method how to retrieve the token from the HTTP request.
		/// </summary>
		public Func<HttpRequest, string> TokenRetriever { get; set; } = TokenRetrieval.FromAuthorizationHeader();

		/// <summary>
		/// Specifies the policy for the discovery client.
		/// </summary>
		public DiscoveryPolicy DiscoveryPolicy { get; set; } = new DiscoveryPolicy();

		/// <summary>
		/// Specifies the timout for contacting the discovery endpoint.
		/// </summary>
		public TimeSpan DiscoveryTimeout { get; set; } = TimeSpan.FromSeconds(60);

		/// <summary>
		/// Specifies the HTTP handler for the discovery endpoint.
		/// </summary>
		public HttpMessageHandler DiscoveryHttpHandler { get; set; }

		/// <summary>
		/// Sets the URL of the token endpoint.
		/// If set, Authority is ignored.
		/// </summary>
		public string TokenEndpoint { get; set; }

		/// <summary>
		/// Specifies the HTTP handler for the token endpoint.
		/// </summary>
		public HttpMessageHandler TokenHttpHandler { get; set; }

		/// <summary>
		/// Authentication style used by the TokenClient.
		/// </summary>
		public AuthenticationStyle AuthenticationStyle { get; set; } = AuthenticationStyle.BasicAuthentication;

		/// <summary>
		/// Specifies the timout for contacting the token endpoint.
		/// </summary>
		public TimeSpan TokenTimeout { get; set; } = TimeSpan.FromSeconds(60);

		internal AsyncLazy<TokenClient> TokenClient { get; set; }
		internal ConcurrentDictionary<string, AsyncLazy<TokenResponse>> LazyTokens { get; set; }

		/// <summary>
		/// Check that the options are valid. Should throw an exception if things are not ok.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// You must either set Authority or TokenEndpoint
		/// or
		/// You must set a ClientId
		/// or
		/// You must set a ClientSecret
		/// or
		/// You must set a Scope
		/// or
		/// You must set a GrantType
		/// or
		/// You must set a TokenRetriever
		/// </exception>
		public void Validate()
		{
			var validationErrors = GetValidationErrors().ToList();
			if (validationErrors.Any())
			{
				throw new InvalidOperationException($"Options are not valid:{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, validationErrors)}");
			}
		}

		private IEnumerable<string> GetValidationErrors()
		{
			if (Authority.IsMissing() && TokenEndpoint.IsMissing())
			{
				yield return $"You must either set {nameof(Authority)} or {nameof(TokenEndpoint)}.";
			}

			if (ClientId.IsMissing())
			{
				yield return $"You must set {nameof(ClientId)}.";
			}

			if (ClientSecret.IsMissing())
			{
				yield return $"You must set {nameof(ClientSecret)}.";
			}

			if (Scope.IsMissing())
			{
				yield return $"You must set {nameof(Scope)}.";
			}

			if (GrantType.IsMissing())
			{
				yield return $"You must set {nameof(GrantType)}.";
			}

			if (TokenRetriever == null)
			{
				yield return $"You must set {nameof(TokenRetriever)}.";
			}
		}
	}
}
