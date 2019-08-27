using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using AnonymousIdentity.Configuration;
using AnonymousIdentity.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using IdentityServer4.Extensions;

namespace AnonymousIdentity.UnitTests.Services.Default
{
    [TestFixture]
    public class SharedUserSessionTests
    {
        #region Fields

        private SharedUserSession _subject;
        private ClaimsPrincipal _user;
        private AuthenticationProperties _props;
        private AnonymousIdentityServerOptions _anonOptions = new AnonymousIdentityServerOptions();
        private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private Mock<IAuthenticationSchemeProvider> _mockAuthenticationSchemeProvider;
        private Mock<IAuthenticationHandlerProvider> _mockAuthenticationHandlerProvider; 
        private Mock<IAuthenticationHandler> _mockAuthenticationHandler;
            
        #endregion

        #region Init

        [SetUp]
        public void Init()
        {
            _user = CreateUser("123");
            _props = new AuthenticationProperties();

            var services = new ServiceCollection();

            services.AddSingleton<IAuthenticationService>(MockHelpers.CreateMockAuthenticationService().Object);
            services.AddAuthentication(auth =>
            {
                auth.DefaultAuthenticateScheme = "foo";
            });

            _mockHttpContextAccessor = MockHelpers.CreateMockHttpContextAccessor(services);
            _mockAuthenticationSchemeProvider = MockHelpers.CreateMockAuthenticationSchemeProvider();
            _mockAuthenticationHandler = MockHelpers.CreateMockAuthenticationHandler();
            _mockAuthenticationHandlerProvider = MockHelpers.CreateMockAuthenticationHandlerProvider(_mockAuthenticationHandler.Object);

            _subject = new SharedUserSession(
                _mockHttpContextAccessor.Object,
                _mockAuthenticationSchemeProvider.Object,
                _mockAuthenticationHandlerProvider.Object,
                _anonOptions,
                MockHelpers.CreateMockSystemClock().Object);
        }
            
        #endregion

        #region CreateSessionId tests
            
        [Test]
        public async Task CreateSessionId_when_user_is_not_authenticated_should_generate_new_sid()
        {
            await _subject.CreateSessionIdAsync(_user, _props);

            _props.Items[SharedUserSession.SessionIdKey].Should().NotBeNull();
        }

        [Test]
        public async Task CreateSessionId_when_user_is_authenticated_should_not_generate_new_sid()
        {
            _props.Items.Add(SharedUserSession.SessionIdKey, "test");
            _mockAuthenticationHandler.Setup(h => h.AuthenticateAsync()).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(_user, _props, MockHelpers.DefaultAuthenticationSchemeName)));

            await _subject.CreateSessionIdAsync(_user, _props);

            _props.Items[SharedUserSession.SessionIdKey].Should().Be("test");
        }

        [Test]
        public async Task CreateSessionId_when_anonymous_user_is_authenticated_and_other_anonymous_user_is_signs_in_should_not_generate_new_sid()
        {
            AddAnonymousAuthenticationMethod(_user);
            _props.Items.Add(SharedUserSession.SessionIdKey, "test");
            _mockAuthenticationHandler.Setup(h => h.AuthenticateAsync()).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(_user, _props, MockHelpers.DefaultAuthenticationSchemeName)));

            var newAnonUserProps = new AuthenticationProperties();
            var newAnonUser = CreateUser("456");
            AddAnonymousAuthenticationMethod(newAnonUser);

            await _subject.CreateSessionIdAsync(newAnonUser, newAnonUserProps);

            _props.Items[SharedUserSession.SessionIdKey].Should().Be("test");
            newAnonUserProps.Items[SharedUserSession.SessionIdKey].Should().Be("test");
        }

        [Test]
        public async Task CreateSessionId_when_anonymous_user_is_authenticated_and_other_user_is_signs_in_should_not_generate_new_sid()
        {
            AddAnonymousAuthenticationMethod(_user);
            _props.Items.Add(SharedUserSession.SessionIdKey, "test");
            _mockAuthenticationHandler.Setup(h => h.AuthenticateAsync()).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(_user, _props, MockHelpers.DefaultAuthenticationSchemeName)));

            var newUserProps = new AuthenticationProperties();
            var newUser = CreateUser("456");

            await _subject.CreateSessionIdAsync(newUser, newUserProps);

            _props.Items[SharedUserSession.SessionIdKey].Should().Be("test");
            newUserProps.Items[SharedUserSession.SessionIdKey].Should().Be("test");
        }

        [Test]
        public async Task CreateSessionId_when_anonymous_user_is_authenticated_and_user_is_signs_in_should_create_aid()
        {
            AddAnonymousAuthenticationMethod(_user);
            _mockAuthenticationHandler.Setup(h => h.AuthenticateAsync()).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(_user, _props, MockHelpers.DefaultAuthenticationSchemeName)));

            var newUser = CreateUser("456");

            _props.Items.ContainsKey(SharedUserSession.AnonymousIdKey).Should().BeFalse();

            await _subject.CreateSessionIdAsync(newUser, _props);

            _props.Items[SharedUserSession.SessionIdKey].Should().NotBeNull();
            _props.Items[SharedUserSession.AnonymousIdKey].Should().NotBeNull();
        }

        [Test]
        public async Task CreateSessionId_when_user_is_authenticated_and_other_anonymous_user_is_signs_in_should_generate_new_sid()
        {
            _props.Items.Add(SharedUserSession.SessionIdKey, "test");
            _mockAuthenticationHandler.Setup(h => h.AuthenticateAsync()).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(_user, _props, MockHelpers.DefaultAuthenticationSchemeName)));

            var newUser = CreateUser("456");
            AddAnonymousAuthenticationMethod(newUser);

            await _subject.CreateSessionIdAsync(newUser, _props);

            _props.Items[SharedUserSession.SessionIdKey].Should().NotBeNull();
            _props.Items[SharedUserSession.SessionIdKey].Should().NotBe("test");
        }

        [Test]
        public async Task CreateSessionId_when_user_is_authenticated_and_other_user_is_signs_in_should_generate_new_sid()
        {
            _props.Items.Add(SharedUserSession.SessionIdKey, "test");
            _mockAuthenticationHandler.Setup(h => h.AuthenticateAsync()).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(_user, _props, MockHelpers.DefaultAuthenticationSchemeName)));

            var newUser = CreateUser("456");

            await _subject.CreateSessionIdAsync(newUser, _props);

            _props.Items[SharedUserSession.SessionIdKey].Should().NotBeNull();
            _props.Items[SharedUserSession.SessionIdKey].Should().NotBe("test");
        }

        [Test]
        public async Task CreateSessionId_when_props_does_not_contain_key_should_generate_new_sid()
        {
            _mockAuthenticationHandler.Setup(h => h.AuthenticateAsync()).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(_user, _props, MockHelpers.DefaultAuthenticationSchemeName)));

            _props.Items.ContainsKey(SharedUserSession.SessionIdKey).Should().Be(false);

            await _subject.CreateSessionIdAsync(_user, _props);

            _props.Items.ContainsKey(SharedUserSession.SessionIdKey).Should().Be(true);
        }

        [Test]
        public async Task CreateSessionId_should_issue_session_id_cookie()
        {
            await _subject.CreateSessionIdAsync(_user, _props);

            var cookieContainer = new CookieContainer();
            var cookies = _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Where(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value);
            cookieContainer.SetCookies(new Uri("http://server"), string.Join(",", cookies));
            _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Clear();

            var cookie = cookieContainer.GetCookies(new Uri("http://server")).Cast<Cookie>().Where(x => x.Name == _anonOptions.CheckSharedSessionCookieName).FirstOrDefault();
            
            cookie.Value.Should().NotBeNull();
            _props.Items[SharedUserSession.SessionIdKey].Should().Be(cookie.Value);
        }

        #endregion

        #region GetSessionId tests

        [Test]
        public async Task GetSessionId_when_user_is_not_authenticated_should_return_null()
        {
            var sid = await _subject.GetSessionIdAsync();

            sid.Should().BeNull();
        }

        [Test]
        public async Task GetSessionId_when_user_is_authenticated_should_return_sid()
        {
            _props.Items.Add(SharedUserSession.SessionIdKey, "test");
            _mockAuthenticationHandler.Setup(h => h.AuthenticateAsync()).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(_user, _props, MockHelpers.DefaultAuthenticationSchemeName)));
            
            var sid = await _subject.GetSessionIdAsync();

            sid.Should().Be("test");
        }
            
        #endregion

        #region EnsureSessionIdCookie tests

        [Test]
        public async Task EnsureSessionIdCookie_should_add_cookie()
        {
            _props.Items.Add(SharedUserSession.SessionIdKey, "test");
            _mockAuthenticationHandler.Setup(h => h.AuthenticateAsync()).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(_user, _props, MockHelpers.DefaultAuthenticationSchemeName)));

            await _subject.EnsureSessionIdCookieAsync();

            var cookieContainer = new CookieContainer();
            var cookies = _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Where(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value);
            cookieContainer.SetCookies(new Uri("http://server"), string.Join(",", cookies));
            _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Clear();

            var cookie = cookieContainer.GetCookies(new Uri("http://server")).Cast<Cookie>().Where(x => x.Name == _anonOptions.CheckSharedSessionCookieName).FirstOrDefault();
            
            cookie.Value.Should().Be("test");
        }

        [Test]
        public async Task EnsureSessionIdCookie_should_not_add_cookie_if_no_sid()
        {
            await _subject.EnsureSessionIdCookieAsync();

            var cookieContainer = new CookieContainer();
            var cookies = _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Where(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value);
            cookieContainer.SetCookies(new Uri("http://server"), string.Join(",", cookies));
            _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Clear();

            var cookie = cookieContainer.GetCookies(new Uri("http://server")).Cast<Cookie>().Where(x => x.Name == _anonOptions.CheckSharedSessionCookieName).FirstOrDefault();

            cookie.Should().BeNull();
        }

        [Test]
        public async Task EnsureSessionIdCookie_should_remove_cookie_if_no_sid_in_properties()
        {
            _props.Items.Add(SharedUserSession.SessionIdKey, "test");
            _mockAuthenticationHandler.Setup(h => h.AuthenticateAsync()).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(_user, _props, MockHelpers.DefaultAuthenticationSchemeName)));

            await _subject.EnsureSessionIdCookieAsync();

            var cookieContainer = new CookieContainer();
            var cookies = _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Where(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value);
            cookieContainer.SetCookies(new Uri("http://server"), string.Join(",", cookies));
            _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Clear();

            string cookie = cookieContainer.GetCookieHeader(new Uri("http://server"));

            cookie.Should().NotBeNull();

            _mockHttpContextAccessor.Object.HttpContext.Request.Headers.Add("Cookie", cookie);

            _props.Items.Clear();
            
            await _subject.EnsureSessionIdCookieAsync();
            
            cookies = _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Where(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value);
            cookieContainer.SetCookies(new Uri("http://server"), string.Join(",", cookies));

            var query = cookieContainer.GetCookies(new Uri("http://server")).Cast<Cookie>().Where(x => x.Name == _anonOptions.CheckSharedSessionCookieName);

            query.Should().BeEmpty();
        }
            
        #endregion

        #region RemoveSessionIdCookie tests

        [Test]
        public async Task RemoveSessionIdCookie_should_remove_cookie()
        {
            _props.Items.Add(SharedUserSession.SessionIdKey, "test");
            _mockAuthenticationHandler.Setup(h => h.AuthenticateAsync()).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(_user, _props, MockHelpers.DefaultAuthenticationSchemeName)));

            await _subject.EnsureSessionIdCookieAsync();

            var cookieContainer = new CookieContainer();
            var cookies = _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Where(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value);
            cookieContainer.SetCookies(new Uri("http://server"), string.Join(",", cookies));
            _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Clear();

            string cookie = cookieContainer.GetCookieHeader(new Uri("http://server"));
            
            cookie.Should().NotBeNull();

            _mockHttpContextAccessor.Object.HttpContext.Request.Headers.Add("Cookie", cookie);

            await _subject.RemoveSessionIdCookieAsync();
            
            cookies = _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Where(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value);
            cookieContainer.SetCookies(new Uri("http://server"), string.Join(",", cookies));

            var query = cookieContainer.GetCookies(new Uri("http://server")).Cast<Cookie>().Where(x => x.Name == _anonOptions.CheckSharedSessionCookieName);
            
            query.Should().BeEmpty();
        }
            
        #endregion

        #region GetAnonymousId tests

        [Test]
        public async Task GetAnonymousId_when_user_is_not_authenticated_should_return_null()
        {
            var aid = await _subject.GetAnonymousIdAsync();

            aid.Should().BeNull();
        }

        [Test]
        public async Task GetAnonymousId_when_user_is_authenticated_and_aid_is_pre_initialized_should_return_null()
        {
            _props.Items.Add(SharedUserSession.AnonymousIdKey, "bar");
            _mockAuthenticationHandler.Setup(h => h.AuthenticateAsync()).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(_user, _props, MockHelpers.DefaultAuthenticationSchemeName)));

            var aid = await _subject.GetAnonymousIdAsync();

            aid.Should().BeNull();
        }

        [Test]
        public async Task GetAnonymousId_when_user_is_anonymous_should_return_null()
        {
            AddAnonymousAuthenticationMethod(_user);
            _mockAuthenticationHandler.Setup(h => h.AuthenticateAsync()).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(_user, _props, MockHelpers.DefaultAuthenticationSchemeName)));
            
            var newUser = CreateUser("456");
            AddAnonymousAuthenticationMethod(newUser);

            await _subject.CreateSessionIdAsync(newUser, _props);

            _props.Items[SharedUserSession.SessionIdKey].Should().NotBeNull();

            var aid = await _subject.GetAnonymousIdAsync();

            aid.Should().BeNull();
        }

        [Test]
        public async Task GetAnonymousId_when_user_is_authenticated_should_return_aid()
        {
            AddAnonymousAuthenticationMethod(_user);
            _mockAuthenticationHandler.Setup(h => h.AuthenticateAsync()).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(_user, _props, MockHelpers.DefaultAuthenticationSchemeName)));

            var newUser = CreateUser("456");

            await _subject.CreateSessionIdAsync(newUser, _props);

            _props.Items[SharedUserSession.SessionIdKey].Should().NotBeNull();
            
            var aid = await _subject.GetAnonymousIdAsync();

            aid.Should().Be("123");
        }

        [Test]
        public async Task GetAnonymousId_when_user_is_authenticated_and_aid_is_corrupted_should_return_null()
        {
            AddAnonymousAuthenticationMethod(_user);
            _mockAuthenticationHandler.Setup(h => h.AuthenticateAsync()).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(_user, _props, MockHelpers.DefaultAuthenticationSchemeName)));

            var newUser = CreateUser("456");

            await _subject.CreateSessionIdAsync(newUser, _props);

            _props.Items[SharedUserSession.SessionIdKey].Should().NotBeNull();
            _props.Items[SharedUserSession.AnonymousIdKey].Should().NotBeNull();
            _props.Items[SharedUserSession.AnonymousIdKey] = "junk";

            var sid =await _subject.GetAnonymousIdAsync();
            
            sid.Should().BeNull();
            _props.Items.ContainsKey(SharedUserSession.AnonymousIdKey).Should().BeFalse();
        }
            
        #endregion

        #region GetUser tests

        [Test]
        public async Task GetUser_when_user_is_not_authenticated_should_return_null()
        {
            var user = await _subject.GetUserAsync();

            user.Should().BeNull();
        }

        [Test]
        public async Task GetUser_when_user_is_authenticated_should_return_user()
        {
            _mockAuthenticationHandler.Setup(h => h.AuthenticateAsync()).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(_user, _props, MockHelpers.DefaultAuthenticationSchemeName)));
            
            var user = await _subject.GetUserAsync();
            
            user.GetSubjectId().Should().Be("123");
        }
            
        #endregion

        #region Utilities

        private void AddAnonymousAuthenticationMethod(ClaimsPrincipal user)
        {
            user.Identities.First().AddClaim(new Claim(
                IdentityModel.JwtClaimTypes.AuthenticationMethod, 
                OidcConstants.AuthenticationMethods.Anonymous));
        }

        private ClaimsPrincipal CreateUser(string subject)
        {
            var claims = new List<Claim>()
            {
                new Claim(IdentityModel.JwtClaimTypes.Subject, subject)
            };
            var id = new ClaimsIdentity(
                claims, 
                "test", 
                IdentityModel.JwtClaimTypes.Name, 
                IdentityModel.JwtClaimTypes.Role);
            return new ClaimsPrincipal(id);
        }
            
        #endregion
    }
}