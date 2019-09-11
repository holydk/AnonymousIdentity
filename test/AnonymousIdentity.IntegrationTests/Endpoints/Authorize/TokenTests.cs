using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace AnonymousIdentity.IntegrationTests.Endpoints.Authorize
{
    public class TokenTests
    {
        #region Fields

        private IdentityServerPipeline _mockPipeline;
            
        #endregion

        #region Init

        [SetUp]
        public void Init()
        {
            _mockPipeline = new IdentityServerPipeline();

            _mockPipeline.Clients.AddRange(new Client[] {
                new Client
                {
                    ClientId = "client1",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    RequireConsent = false,
                    AllowedScopes = new List<string> { "openid", "profile", "aid" },
                    RedirectUris = new List<string> { "https://client1/callback" },
                    AllowAccessTokensViaBrowser = true,
                    AccessTokenLifetime = 3600,
                    IdentityTokenLifetime = 500
                },
                new Client
                {
                    ClientId = "client2",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    RequireConsent = true,
                    AllowedScopes = new List<string> { "openid", "profile", "api1", "api2", "aid", "aid_idResource" },
                    RedirectUris = new List<string> { "https://client2/callback" },
                    AllowAccessTokensViaBrowser = true,
                    AlwaysIncludeUserClaimsInIdToken = true
                }
            });

            _mockPipeline.Users.Add(new TestUser
            {
                SubjectId = "bob",
                Username = "bob",
                Claims = new Claim[]
                {
                    new Claim("name", "Bob Loblaw"),
                    new Claim("email", "bob@loblaw.com"),
                    new Claim("role", "Attorney")
                }
            });

            _mockPipeline.IdentityScopes.AddRange(new IdentityResource[] {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResource("aid_idResource", new [] { "aid" })
            });
            _mockPipeline.ApiScopes.AddRange(new ApiResource[] {
                new ApiResource
                {
                    Name = "api",
                    Scopes =
                    {
                        new Scope
                        {
                            Name = "api1"
                        },
                        new Scope
                        {
                            Name = "api2"
                        },
                        new Scope
                        {
                            Name = "aid",
                            UserClaims = new List<string>() { "aid" }
                        }
                    }
                }
            });

            _mockPipeline.Initialize();
        }
            
        #endregion

        [Test]
        public async Task Shared_session_id_should_be_included_in_anonymous_access_token_always()
        {
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client2",
                responseType: "id_token token",
                scope: "openid api2",
                redirectUri: "https://client2/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var token = _mockPipeline.ReadJwtToken(((string)result["access_token"]));

            token.Claims.Should().Contain(c => c.Type == JwtClaimTypes.SharedSessionId);
        }

        [Test]
        public async Task Shared_session_id_should_not_be_included_in_anonymous_access_token_if_not_requested()
        {
            _mockPipeline.AnonymousOptions.IncludeSharedSessionIdInAccessToken = false;
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client2",
                responseType: "id_token token",
                scope: "openid api2",
                redirectUri: "https://client2/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var token = _mockPipeline.ReadJwtToken(((string)result["access_token"]));

            token.Claims.Should().NotContain(c => c.Type == JwtClaimTypes.SharedSessionId);
        }

        [Test]
        public async Task Shared_session_id_should_be_included_in_authenticated_access_token()
        {
            await _mockPipeline.LoginAsync("bob");
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client2",
                responseType: "id_token token",
                scope: "openid api2",
                redirectUri: "https://client2/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var token = _mockPipeline.ReadJwtToken(((string)result["access_token"]));

            token.Claims.Should().Contain(c => c.Type == JwtClaimTypes.SharedSessionId);
        }

        [Test]
        [TestCase("access_token")]
        [TestCase("id_token")]
        public async Task Anonymous_token_should_contain_anonymous_authentication_method(string tokenName)
        {
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client2",
                responseType: "id_token token",
                scope: "openid api2",
                redirectUri: "https://client2/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var token = _mockPipeline.ReadJwtToken(((string)result[tokenName]));

            token.Claims.Should().Contain(c => c.Type == IdentityModel.JwtClaimTypes.AuthenticationMethod);
            token.Claims.First(c => c.Type == IdentityModel.JwtClaimTypes.AuthenticationMethod).
                Value.Should().Be(OidcConstants.AuthenticationMethods.Anonymous);
        }

        [Test]
        [TestCase("access_token")]
        [TestCase("id_token")]
        public async Task Authenticated_token_should_contain_pwd_authentication_method(string tokenName)
        {
            await _mockPipeline.LoginAsync("bob");
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client2",
                responseType: "id_token token",
                scope: "openid api2",
                redirectUri: "https://client2/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var token = _mockPipeline.ReadJwtToken(((string)result[tokenName]));

            token.Claims.Should().Contain(c => c.Type == IdentityModel.JwtClaimTypes.AuthenticationMethod);
            token.Claims.First(c => c.Type == IdentityModel.JwtClaimTypes.AuthenticationMethod).
                Value.Should().Be(IdentityModel.OidcConstants.AuthenticationMethods.Password);
        }

        [Test]
        [TestCase("access_token")]
        [TestCase("id_token")]
        public async Task When_anonymous_user_is_authenticated_and_user_signs_in_token_should_contain_pwd_authentication_method(string tokenName)
        {
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client2",
                responseType: "id_token token",
                scope: "openid api2",
                redirectUri: "https://client2/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var token = _mockPipeline.ReadJwtToken(((string)result[tokenName]));

            token.Claims.Should().Contain(c => c.Type == IdentityModel.JwtClaimTypes.AuthenticationMethod);
            token.Claims.First(c => c.Type == IdentityModel.JwtClaimTypes.AuthenticationMethod).
                Value.Should().Be(OidcConstants.AuthenticationMethods.Anonymous);

            await _mockPipeline.LoginAsync("bob");

            response = await _mockPipeline.BrowserClient.GetAsync(url);
            result = JObject.Parse(await response.Content.ReadAsStringAsync());
            token = _mockPipeline.ReadJwtToken(((string)result[tokenName]));

            token.Claims.Should().Contain(c => c.Type == IdentityModel.JwtClaimTypes.AuthenticationMethod);
            token.Claims.First(c => c.Type == IdentityModel.JwtClaimTypes.AuthenticationMethod).
                Value.Should().Be(IdentityModel.OidcConstants.AuthenticationMethods.Password);
        }

        [Test]
        [TestCase("access_token")]
        [TestCase("id_token")]
        public async Task Authenticated_token_should_not_contain_aid_if_client_not_requested_anonymous_token(string tokenName)
        {
            await _mockPipeline.LoginAsync("bob");
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client2",
                responseType: "id_token token",
                scope: "openid api2",
                redirectUri: "https://client2/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var token = _mockPipeline.ReadJwtToken(((string)result[tokenName]));

            token.Claims.Should().NotContain(c => c.Type == JwtClaimTypes.AnonymousId);
        }

        [Test]
        [TestCase("access_token")]
        [TestCase("id_token")]
        public async Task Authenticated_token_should_contain_aid_if_client_requested_anonymous_token_and_aid_always_include_in_access_token(string tokenName)
        {
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client2",
                responseType: "id_token token",
                scope: "openid api2",
                redirectUri: "https://client2/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var token = _mockPipeline.ReadJwtToken(((string)result[tokenName]));

            var anonymousSubject = token.Subject;

            await _mockPipeline.LoginAsync("bob");

            response = await _mockPipeline.BrowserClient.GetAsync(url);
            result = JObject.Parse(await response.Content.ReadAsStringAsync());
            token = _mockPipeline.ReadJwtToken(((string)result[tokenName]));

            token.Claims.Should().Contain(c => c.Type == JwtClaimTypes.AnonymousId);
            token.Claims.First(c => c.Type == JwtClaimTypes.AnonymousId).
                Value.Should().Be(anonymousSubject);
        }

        [Test]
        [TestCase("access_token")]
        [TestCase("id_token")]
        public async Task Authenticated_token_should_not_contain_aid_if_client_requested_anonymous_token_and_not_requested_scope_aid(string tokenName)
        {
            _mockPipeline.AnonymousOptions.AlwaysIncludeAnonymousIdInProfile = false;
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client2",
                responseType: "id_token token",
                scope: "openid api2",
                redirectUri: "https://client2/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var token = _mockPipeline.ReadJwtToken(((string)result[tokenName]));

            await _mockPipeline.LoginAsync("bob");

            response = await _mockPipeline.BrowserClient.GetAsync(url);
            result = JObject.Parse(await response.Content.ReadAsStringAsync());
            token = _mockPipeline.ReadJwtToken(((string)result[tokenName]));

            token.Claims.Should().NotContain(c => c.Type == JwtClaimTypes.AnonymousId);
        }

        [Test]
        public async Task Authenticated_access_token_should_contain_aid_if_client_requested_anonymous_token_and_requested_scope_aid()
        {
            _mockPipeline.AnonymousOptions.AlwaysIncludeAnonymousIdInProfile = false;
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client2",
                responseType: "id_token token",
                scope: "openid api2 aid",
                redirectUri: "https://client2/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var token = _mockPipeline.ReadJwtToken(((string)result["access_token"]));

            var anonymousSubject = token.Subject;

            await _mockPipeline.LoginAsync("bob");

            response = await _mockPipeline.BrowserClient.GetAsync(url);
            result = JObject.Parse(await response.Content.ReadAsStringAsync());
            token = _mockPipeline.ReadJwtToken(((string)result["access_token"]));

            token.Claims.Should().Contain(c => c.Type == JwtClaimTypes.AnonymousId);
            token.Claims.First(c => c.Type == JwtClaimTypes.AnonymousId).
                Value.Should().Be(anonymousSubject);
        }

        [Test]
        public async Task Authenticated_id_token_should_contain_aid_if_client_requested_anonymous_token_and_requested_scope_aid()
        {
            _mockPipeline.AnonymousOptions.AlwaysIncludeAnonymousIdInProfile = false;
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client2",
                responseType: "id_token token",
                scope: "openid api2 aid_idResource",
                redirectUri: "https://client2/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var token = _mockPipeline.ReadJwtToken(((string)result["id_token"]));

            var anonymousSubject = token.Subject;

            await _mockPipeline.LoginAsync("bob");

            response = await _mockPipeline.BrowserClient.GetAsync(url);
            result = JObject.Parse(await response.Content.ReadAsStringAsync());
            token = _mockPipeline.ReadJwtToken(((string)result["id_token"]));

            token.Claims.Should().Contain(c => c.Type == JwtClaimTypes.AnonymousId);
            token.Claims.First(c => c.Type == JwtClaimTypes.AnonymousId).
                Value.Should().Be(anonymousSubject);
        }

        [Test]
        public async Task Anonymous_access_token_should_contain_valid_expires_id()
        {
            _mockPipeline.AnonymousOptions.AccessTokenLifetime = 5000;
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var token = _mockPipeline.ReadJwtToken(((string)result["access_token"]));

            var nbf = int.Parse(token.Claims.First(c => c.Type == IdentityModel.JwtClaimTypes.NotBefore).Value);
            var exp = int.Parse(token.Claims.First(c => c.Type == IdentityModel.JwtClaimTypes.Expiration).Value);

            (exp - nbf).Should().Be(5000);
        }

        [Test]
        public async Task Anonymous_id_token_should_contain_valid_expires_id()
        {
            _mockPipeline.AnonymousOptions.IdentityTokenLifetime = 5000;
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var token = _mockPipeline.ReadJwtToken(((string)result["id_token"]));

            var nbf = int.Parse(token.Claims.First(c => c.Type == IdentityModel.JwtClaimTypes.NotBefore).Value);
            var exp = int.Parse(token.Claims.First(c => c.Type == IdentityModel.JwtClaimTypes.Expiration).Value);

            (exp - nbf).Should().Be(5000);
        }

        [Test]
        public async Task Authenticated_access_token_should_contain_valid_expires_id()
        {
            _mockPipeline.AnonymousOptions.AccessTokenLifetime = 5000;
            await _mockPipeline.LoginAsync("bob");
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var token = _mockPipeline.ReadJwtToken(((string)result["access_token"]));

            var nbf = int.Parse(token.Claims.First(c => c.Type == IdentityModel.JwtClaimTypes.NotBefore).Value);
            var exp = int.Parse(token.Claims.First(c => c.Type == IdentityModel.JwtClaimTypes.Expiration).Value);

            (exp - nbf).Should().Be(3600);
        }

        [Test]
        public async Task Authenticated_id_token_should_contain_valid_expires_id()
        {
            _mockPipeline.AnonymousOptions.IdentityTokenLifetime = 5000;
            await _mockPipeline.LoginAsync("bob");
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var token = _mockPipeline.ReadJwtToken(((string)result["id_token"]));

            var nbf = int.Parse(token.Claims.First(c => c.Type == IdentityModel.JwtClaimTypes.NotBefore).Value);
            var exp = int.Parse(token.Claims.First(c => c.Type == IdentityModel.JwtClaimTypes.Expiration).Value);

            (exp - nbf).Should().Be(500);
        }

        [Test]
        public async Task Authenticated_id_token_should_not_contain_aid_if_client_requested_anonymous_token_and_aid_always_include_in_id_token_but_user_claims_not_includes_in_id_token()
        {
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client1",
                responseType: "id_token token",
                scope: "openid",
                redirectUri: "https://client1/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            await _mockPipeline.LoginAsync("bob");

            response = await _mockPipeline.BrowserClient.GetAsync(url);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var token = _mockPipeline.ReadJwtToken(((string)result["id_token"]));

            token.Claims.Should().NotContain(c => c.Type == JwtClaimTypes.AnonymousId);
        }
    }
}