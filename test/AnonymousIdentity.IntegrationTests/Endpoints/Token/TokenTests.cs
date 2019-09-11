using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityModel.Client;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace AnonymousIdentity.IntegrationTests.Endpoints.Token
{
    public class TokenTests
    {
        #region Fields

        private IdentityServerPipeline _mockPipeline;
        private Client _client1;

        private string client_id = "client";
        private string client_secret = "secret";

        private string scope_name = "api3";
        private string scope_secret = "api_secret";
            
        #endregion

        #region Init

        [SetUp]
        public void Init()
        {
            _mockPipeline = new IdentityServerPipeline();

            _mockPipeline.Clients.AddRange(new Client[] {
                _client1 = new Client
                {
                    ClientId = "client1",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    RequireConsent = false,
                    AllowedScopes = new List<string> { "openid", "profile" },
                    RedirectUris = new List<string> { "https://client1/callback" },
                    AllowAccessTokensViaBrowser = true
                },
                new Client
                {
                    ClientId = "client2",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    RequireConsent = true,
                    AllowedScopes = new List<string> { "openid", "profile", "api1", "api2" },
                    RedirectUris = new List<string> { "https://client2/callback" },
                    AllowAccessTokensViaBrowser = true
                },
                new Client
                {
                    ClientId = "client3",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    RequireConsent = false,
                    AllowedScopes = new List<string> { "openid", "profile", "api1", "api2" },
                    RedirectUris = new List<string> { "https://client3/callback" },
                    AllowAccessTokensViaBrowser = true,
                    EnableLocalLogin = false,
                    IdentityProviderRestrictions = new List<string> { "google" }
                },
                new Client
                {
                    ClientId = "client4",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequireClientSecret = false,
                    RequireConsent = false,
                    AllowedScopes = new List<string> { "openid", "profile", "api1", "api2" },
                    RedirectUris = new List<string> { "https://client4/callback" },
                },
                new Client
                {
                    ClientId = client_id,
                    ClientSecrets = new List<Secret> { new Secret(client_secret.Sha256()) },
                    AllowedGrantTypes = { GrantType.ClientCredentials, GrantType.ResourceOwnerPassword },
                    AllowedScopes = new List<string> { "api3" },
                }
            });

            _mockPipeline.Users.Add(new TestUser
            {
                SubjectId = "bob",
                Username = "bob",
                Password = "password",
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
                new IdentityResources.Email()
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
                        }
                    }
                },
                new ApiResource
                {
                    Name = "api1",
                    ApiSecrets = new List<Secret> { new Secret(scope_secret.Sha256()) },
                    Scopes =
                    {
                        new Scope
                        {
                            Name = scope_name
                        }
                    }
                }
            });

            _mockPipeline.Initialize();
        }
            
        #endregion
    
        [Test]
        public async Task Anonymous_user_with_valid_request_should_receive_authorization_response()
        {
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client4",
                responseType: "code",
                scope: "openid",
                redirectUri: "https://client4/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            var result = JObject.Parse(await response.Content.ReadAsStringAsync());

            var tokenResponse = await _mockPipeline.BrowserClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest()
            {
                Code = (string)result["code"],
                RedirectUri = "https://client4/callback",
                Address = IdentityServerPipeline.TokenEndpoint,
                ClientId = "client4",
            });

            tokenResponse.IsError.Should().BeFalse();
            tokenResponse.AccessToken.Should().NotBeNull();
            tokenResponse.IdentityToken.Should().NotBeNull();
        }

        [Test]
        public async Task Anonymous_user_with_valid_request_should_receive_authorization_response_with_valid_access_token_expires_in()
        {
            _mockPipeline.AnonymousOptions.AccessTokenLifetime = 5000;
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client4",
                responseType: "code",
                scope: "openid",
                redirectUri: "https://client4/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            var result = JObject.Parse(await response.Content.ReadAsStringAsync());

            var tokenResponse = await _mockPipeline.BrowserClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest()
            {
                Code = (string)result["code"],
                RedirectUri = "https://client4/callback",
                Address = IdentityServerPipeline.TokenEndpoint,
                ClientId = "client4",
            });

            tokenResponse.ExpiresIn.Should().Be(5000);
        }

        [Test]
        public async Task Authenticated_user_with_valid_anonymous_request_should_receive_authorization_response_with_valid_access_token_expires_in()
        {
            await _mockPipeline.LoginAsync("bob");
            
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client4",
                responseType: "code",
                scope: "openid",
                redirectUri: "https://client4/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            var result = JObject.Parse(await response.Content.ReadAsStringAsync());

            var tokenResponse = await _mockPipeline.BrowserClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest()
            {
                Code = (string)result["code"],
                RedirectUri = "https://client4/callback",
                Address = IdentityServerPipeline.TokenEndpoint,
                ClientId = "client4",
            });

            tokenResponse.ExpiresIn.Should().Be(3600);
        }

        [Test]
        public async Task client_credentials_request_with_funny_headers_should_not_hang()
        {
            var data = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", client_id },
                { "client_secret", client_secret },
                { "scope", scope_name },
            };
            var form = new FormUrlEncodedContent(data);
            _mockPipeline.BackChannelClient.DefaultRequestHeaders.Add("Referer", "http://127.0.0.1:33086/appservice/appservice?t=1564165664142?load");
            var response = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.TokenEndpoint, form);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var json = await response.Content.ReadAsStringAsync();
            var result = JObject.Parse(json);
            result.ContainsKey("error").Should().BeFalse();
        }

        [Test]
        public async Task resource_owner_request_with_funny_headers_should_not_hang()
        {
            var data = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "username", "bob" },
                { "password", "password" },
                { "client_id", client_id },
                { "client_secret", client_secret },
                { "scope", scope_name },
            };
            var form = new FormUrlEncodedContent(data);
            _mockPipeline.BackChannelClient.DefaultRequestHeaders.Add("Referer", "http://127.0.0.1:33086/appservice/appservice?t=1564165664142?load");
            var response = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.TokenEndpoint, form);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var json = await response.Content.ReadAsStringAsync();
            var result = JObject.Parse(json);
            result.ContainsKey("error").Should().BeFalse();
        }

        [Test]
        public async Task client_credentials_request_with_funny_headers_should_have_valid_expires_in()
        {
            var data = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", client_id },
                { "client_secret", client_secret },
                { "scope", scope_name },
            };
            var form = new FormUrlEncodedContent(data);
            _mockPipeline.BackChannelClient.DefaultRequestHeaders.Add("Referer", "http://127.0.0.1:33086/appservice/appservice?t=1564165664142?load");
            var response = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.TokenEndpoint, form);

            var json = await response.Content.ReadAsStringAsync();
            var result = JObject.Parse(json);

            ((int)result["expires_in"]).Should().Be(3600);
        }

        [Test]
        public async Task resource_owner_request_with_funny_headers_should_have_valid_expires_in()
        {
            var data = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "username", "bob" },
                { "password", "password" },
                { "client_id", client_id },
                { "client_secret", client_secret },
                { "scope", scope_name },
            };
            var form = new FormUrlEncodedContent(data);
            _mockPipeline.BackChannelClient.DefaultRequestHeaders.Add("Referer", "http://127.0.0.1:33086/appservice/appservice?t=1564165664142?load");
            var response = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.TokenEndpoint, form);

            var json = await response.Content.ReadAsStringAsync();
            var result = JObject.Parse(json);

            ((int)result["expires_in"]).Should().Be(3600);
        }

        [Test]
        public async Task When_anonymous_user_is_authenticated_and_user_signs_in_with_code_flow_should_return_authorization_response_with_valid_access_token_expires_in()
        {
            _mockPipeline.AnonymousOptions.AccessTokenLifetime = 5000;
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "client4",
                responseType: "code",
                scope: "openid",
                redirectUri: "https://client4/callback",
                state: "123_state",
                nonce: "123_nonce",
                acrValues: "0",
                responseMode: "json");
            var response = await _mockPipeline.BrowserClient.GetAsync(url);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());

            var tokenResponse = await _mockPipeline.BrowserClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest()
            {
                Code = (string)result["code"],
                RedirectUri = "https://client4/callback",
                Address = IdentityServerPipeline.TokenEndpoint,
                ClientId = "client4",
            });

            tokenResponse.ExpiresIn.Should().Be(5000);

            await _mockPipeline.LoginAsync("bob");

            response = await _mockPipeline.BrowserClient.GetAsync(url);
            result = JObject.Parse(await response.Content.ReadAsStringAsync());

            tokenResponse = await _mockPipeline.BrowserClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest()
            {
                Code = (string)result["code"],
                RedirectUri = "https://client4/callback",
                Address = IdentityServerPipeline.TokenEndpoint,
                ClientId = "client4",
            });

            tokenResponse.ExpiresIn.Should().Be(3600);
        }
    }
}