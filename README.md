# AnonymousIdentity
The open source framework based on [IdentityServer4](https://github.com/IdentityServer/IdentityServer4) for anonymous token support.

## How to build
AnonymousIdentity is built against the latest ASP.NET Core 3.

* [Install](https://www.microsoft.com/net/download/core#/current) the latest .NET Core 3.x SDK
* Run `build.ps1` in the root of the repo

## Getting Started
* Install nuget package to a new or existing project
```sh
Install-Package AnonymousIdentity
```
* Add anonymous authentication to the identity server builder after all registration (it's necessary)
```csharp
services.AddIdentityServer()
   // other registrations
   .AddAnonymousAuthentication();
```
* Done

## How it works
- The request for anonymous token to the authorize endpoint looks like this
  
  - Implicit flow
   ```sh
   GET /connect/authorize?
       client_id=client1&
       scope=openid email api1&
       response_type=id_token token&
       redirect_uri=https://myapp/callback&
       state=abc&
       nonce=xyz&
       acr_values=0&
       response_mode=json
   ```
   - Code flow (PKCE is optional)
   ```sh
   GET /connect/authorize?
       client_id=client1&
       scope=openid email api1&
       response_type=code&
       redirect_uri=https://myapp/callback&
       state=abc&
       nonce=xyz&
       acr_values=0&
       response_mode=json
   ```
The difference between a regular and anonymous request in two parameters: acr_values=0 and response_mode=json.
```
acr_values=0
```
The value "0" indicates the End-User authentication did not meet the requirements of [ISO/IEC 29115](https://openid.net/specs/openid-connect-core-1_0.html#ISO29115) [ISO29115] level 1. it is equivalent to an anonymous user.
```
response_mode=json
```
Json for friendly response without redirect.
* The response will look like this (the code flow must exchange the code for a token first)
```
{
  "id_token":<id_token>,
  "access_token":<access_token>,
  "token_type":"Bearer",
  "expires_in":"2592000",
  "scope":"openid email api1",
  "state":"abc"
}
```
Depending on authentication state, either an authenticated or anonymous token is returned.

* The anonymous access token looks like this
```
{
  "nbf": 1566849147,
  "exp": 1569441147,
  "iss": "https://server",
  "aud": [
    "https://server/resources",
    "api"
  ],
  "client_id": "client1",
  "sub": "abda9006-5991-4c90-a88c-c96764027347",
  "auth_time": 1566849147,
  "idp": "local",
  "ssid": "9e6453dbaf5ffdb03f08812f759d3cdf",
  "scope": [
    "openid",
    "email",
    "api1"
  ],
  "amr": [
    "anon"
  ]
}
```
Here, authentication method is anon. Claim ssid(shared session id) will be included in authenticated token, when the user will log in.

* The authenticated access token looks like this
```
{
  "nbf": 1566850295,
  "exp": 1566853895,
  "iss": "https://server",
  "aud": [
    "https://server/resources",
    "api"
  ],
  "client_id": "client1",
  "sub": "bob",
  "auth_time": 1566850295,
  "idp": "local",
  "aid": "abda9006-5991-4c90-a88c-c96764027347",
  "ssid": "9e6453dbaf5ffdb03f08812f759d3cdf",
  "scope": [
    "openid",
    "email",
    "api1"
  ],
  "amr": [
    "pwd"
  ]
}
```
Here, claim aid(anonymous id) equal claim sub from anonymous token, also claim ssid equal claim ssid from anonymous token.

Check [Options](https://github.com/holydk/AnonymousIdentity/blob/master/src/Configuration/DependencyInjection/Options/AnonymousIdentityServerOptions.cs) to configure the anonymous token.

## Contributing
I'm very much open to any collaboration and contributions to this tool to enable additional scenarios. Pull requests are welcome, though please open an issue to discuss first. Security reviews are also much appreciated!