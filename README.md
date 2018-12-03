# AspNetCore.NonInteractiveOidcHandlers

This library offers a set of `DelegatingHandler` subclasses for various `HttpClient` non-interactive
authentication scenarii using [Open ID Connect](https://openid.net/connect/).

> Some of the code in this library was directly copied from the 
> [IdentityModel.AspNetCore.OAuth2Introspection](https://github.com/IdentityModel/IdentityModel.AspNetCore.OAuth2Introspection) library.

[![Build Status](https://manuelguilbault.visualstudio.com/AspNetCore.NonInteractiveOidcHandlers/_apis/build/status/manuel-guilbault.AspNetCore.NonInteractiveOidcHandlers)](https://manuelguilbault.visualstudio.com/AspNetCore.NonInteractiveOidcHandlers/_build/latest?definitionId=2)

## Asp .NET Core

This library exposes a bunch of extension methods on `IHttpClientBuilder` to 
add a `DelegatingHandler` to an `HttpClient` pipeline. The delegating handler will make sure to 
acquire an access token using the proper grant type and the provided information and inject the acquired 
token as `Bearer` in the HTTP request's `Authorization` header.

**Example:**

```csharp
services
  .AddHttpClient("client-for-my-api")
  .AddOidcClientCredentials(options =>
  {
    options.Authority = "https://my-oidc-server";
    options.ClientId = "my-client-id";
    options.ClientSecret = "my-client-secret";
    options.Scope = "my-api-scope";
    options.EnableCaching = true;
    options.CacheDuration = TimeSpan.FromMinutes(10);
  })
;
```

In this example, the `HttpClient` named `client-for-my-api` will be injected a delegating handler which
will acquire an access token using the `client_credentials` grant type when a first HTTP request is made. 
This access token will be cached for a maximum duration of 10 minutes, unless the token's expiration is shorter,
in which case it will be cached until the token expires. Every subsequent request will reuse the cached token.
A new token will be acquired every time a new request is sent and no token is fresh in cache.

## Common options

The options all have the following common properties:

### `Authority`

The URL of the OIDC server. Required.

### `ClientId`

The client ID used to authenticate the token request. Required.

### `ClientSecret`

The client's secret used to authenticate the token request. Required.

### `AuthorityHttpClientAccessor`

A lambda returning an `HttpClient`. The lambda will be call before every request to 
the OIDC server, and the returned `HttpClient` instance will be used for the request.
Default to a lambda which creates a new instance of `HttpClient` every time it is called.

### `DiscoveryPolicy`

The `DiscoveryPolicy` used by the underlying [IdentityModel](https://github.com/IdentityModel/IdentityModel2)
API when requesting the discovery document. Default to the IdentityModel's default DiscoveryPolicy.

### `TokenEndpoint`

The OAuth2 token endpoint. If set, the `Authority` property is ignored, and no discovery request
happens, since the delegating handler will directly know where to request tokens.

### `Events`

Token acquisition events. When the delegating handler tries to acquire a new
access token, it will either call the `OnTokenAcquired` event or the `OnTokenRequestFailed` event,
depending on the outcome.

Listening to the `OnTokenRequestFailed` event doesn't prevent the delegating handler from throwing
an exception when a token request fails.

For exemple, the `OnTokenAcquired` event can be useful to retrieve the new refresh token when using the
`refresh_token` grant type and when the OIDC server renews the refresh token every time an access token
is requested for it (e.g. when Identity Server's
[RefreshTokenUsage](https://identityserver4.readthedocs.io/en/release/topics/refresh_tokens.html)
is set to `OneTime`).

## Caching

In addition to the properties described above, all options have properties to control the caching
of acquired access tokens:

### `EnableCaching`

When set to `true`, the delegating handler will cache acquired tokens. An `IDistributedCache` service
must be registered. Default to `false`.

### `CacheKeyPrefix`

All cache keys will be prefixed with this value. Default to an empty string.

### `CacheDuration`

The maximum duration for which an access token can be cached. The delegating handler will use the smallest
value between this property and the token's expiration. Default to `TimeSpan.MaxValue` (which means the
token's expiration is used by default).

### `TokenExpirationDelay`

How much time before the token's expiration should the cache entry expire. Used when calculating the cache 
entry expiration to compare with the CacheDuration property. Default to 1 minute.

## Supported non-interactive grant types

Below is a list of the supported non-interactive grant types and the extension method to use to register
their delegating handler.

### `client_credentials`

The `client_credentials` grant type is typically used for machine-to-machine, userless authentication.
It produces an access token without any user information.

```csharp
services
  .AddHttpClient("client-for-my-api")
  .AddOidcClientCredentials(options =>
  {
    options.Authority = "https://my-oidc-server";
    options.ClientId = "my-client-id";
    options.ClientSecret = "my-client-secret";
    options.Scope = "my-api-scope";
  })
;
```

### `password`

The `password` grant type requires a username and a password, and produces an access token for
the matching user. It is the least secure grant type and should be used only when nothing else
can.

The option's `UserCredentialsRetriever` property must be set to a lambda which receives an
`IServiceProvider` and returns a `(string userName, string password)?` nullable tuple. The 
delegating handler will call this lambda before trying to acquire a token. A `null` tuple can 
be returned, in which case the delegating handler won't request any token and won't authenticate 
the request.

```csharp
services
  .AddHttpClient("client-for-my-api")
  .AddOidcPassword(options =>
  {
    options.Authority = "https://my-oidc-server";
    options.ClientId = "my-client-id";
    options.ClientSecret = "my-client-secret";
    options.Scope = "my-api-scope";
    options.UserCredentialsRetriever = (serviceProvider) => ("my-username", "my-password");
  })
;
```

### `refresh_token`

The `refresh_token` grant type requires a refresh token that was previously produced for a
specific user, scope and client, and produces an access token for its user.

The options's `RefreshTokenRetriever` property must be set to a lambda which receives an
`IServiceProvider` and returns a refresh token as a `string`. The delegating handler will 
call this lambda before trying to  acquire a token. A `null` value can be returned, in which 
case the delegating handler won't request any token and won't authenticate the request.

```csharp
services
  .AddHttpClient("client-for-my-api")
  .AddOidcRefreshToken(options =>
  {
    options.Authority = "https://my-oidc-server";
    options.ClientId = "my-client-id";
    options.ClientSecret = "my-client-secret";
    options.RefreshTokenRetriever = (serviceProvider) => "my-refresh-token";
  })
;
```

### `delegation`

The `delegation` grant type is a custom grant type which can be used to request a new
access token for a different scope from an existing access token. It is typically used
when an API (dubbed *upstream*) must call another API (dubbed *downstream*), and when both
APIs require different scopes.

This use case is described in the Identity Server 
[documentation](https://identityserver4.readthedocs.io/en/release/topics/extension_grants.html#example-simple-delegation-using-an-extension-grant).

In addition to registering the delegating handler, the `AddOidcTokenDelegation` extension method will
make sure the `IHttpContextAccessor` service is registered, as it uses the current `IHttpContext` to
look for an inbound `Bearer` token in the incoming request's `Authorization` header. If no inbound
token is found, the delegating handler won't request any token and won't authenticate the outgoing
request.

```csharp
services
  .AddHttpClient("client-for-my-downstream-api")
  .AddOidcTokenDelegation(options =>
  {
    options.Authority = "https://my-oidc-server";
    options.ClientId = "my-upstream-api-client-id";
    options.ClientSecret = "my-upstream-api-client-secret";
    options.Scope = "my-downstream-api-scope";
  })
;
```

## Access token pass-through

In addition to the aforementioned strategies, an ASP .NET Core application may simply need to pass its inbound 
access token through to any downstream API call. When applied in the context of an API calling another API, 
this is called *poor man’s delegation* (see 
[Identity Server's doc](https://identityserver4.readthedocs.io/en/release/topics/extension_grants.html#example-simple-delegation-using-an-extension-grant)
).

In the context of a web application using the `authorization_code` or the `hybird` grant type, such as an MVC
application, it can make perfect sense to simply pass through the access token of the current authenticated user
to downstream API calls, hence this extension method:

```csharp
services
    .AddHttpClient("client-for-my-downstream-api")
    .AddAccessTokenPassThrough()
;
```

Here, the `HttpClient` instance named `client-for-my-downstream-api` will have in its pipeline a 
delegating handler which will try to retrieve a token named `access_token` from the request's authentication
ticket and, if any, will inject its value as `Bearer` in the outbound HTTP request's `Authorization` header.
