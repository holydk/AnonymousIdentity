using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityServer4.Anonymous.Configuration;
using IdentityServer4.Anonymous.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Moq;
using NUnit.Framework;

namespace IdentityServer4.Anonymous.UnitTests.Services.Default
{
    [TestFixture]
    public class CookieAnonymousUserManagerTests
    {
        #region Fields

        private CookieAnonymousUserManager _subject;
        private AnonymousIdentityServerOptions _anonOptions = new AnonymousIdentityServerOptions();
        private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private Mock<IAnonymousUserFactory> _mockAnonymousUserFactory;
        private Mock<ISharedUserSession> _mockSharedUserSession;
            
        #endregion

        #region Init

        [SetUp]
        public void Init()
        {
            _mockHttpContextAccessor = MockHelpers.CreateMockHttpContextAccessor();
            _mockAnonymousUserFactory = MockHelpers.CreateMockAnonymousUserFactory();
            _mockSharedUserSession = MockHelpers.CreateMockSharedUserSession();

            _subject = new CookieAnonymousUserManager(
                _mockHttpContextAccessor.Object,
                _mockAnonymousUserFactory.Object,
                _mockSharedUserSession.Object,
                _anonOptions
            );
        }
            
        #endregion

        #region Create tests

        [Test]
        public async Task Create_should_create_cookie()
        {
            await _subject.CreateAsync(new AnonymousUser() { Id = "test" });

            var cookieContainer = new CookieContainer();
            var cookies = _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Where(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value);
            cookieContainer.SetCookies(new Uri("http://server"), string.Join(",", cookies));
            _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Clear();

            var cookie = cookieContainer.GetCookies(new Uri("http://server")).Cast<Cookie>().Where(x => x.Name == _anonOptions.CheckAnonymousIdCookieName).FirstOrDefault();

            cookie.Value.Should().Be("test");
        }

        [Test]
        public async Task Create_should_create_new_user_if_no_user_present()
        {
            _mockAnonymousUserFactory.Setup(m => m.CreateAsync(It.IsAny<string>())).ReturnsAsync(new AnonymousUser() { Id = "test" });

            await _subject.CreateAsync();

            var cookieContainer = new CookieContainer();
            var cookies = _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Where(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value);
            cookieContainer.SetCookies(new Uri("http://server"), string.Join(",", cookies));
            _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Clear();

            var cookie = cookieContainer.GetCookies(new Uri("http://server")).Cast<Cookie>().Where(x => x.Name == _anonOptions.CheckAnonymousIdCookieName).FirstOrDefault();

            cookie.Value.Should().Be("test");
        }

        [Test]
        public async Task Create_should_generate_exception_if_no_user_present_and_factory_dont_creates_user()
        {
            Func<Task> f = () => _subject.CreateAsync();

            await f.Should().ThrowAsync<InvalidOperationException>();
        }
            
        #endregion

        #region FindById tests

        [Test]
        public async Task FindById_when_no_anonymous_id_cookie_and_no_aid_in_session_should_return_null()
        {
            var user = await _subject.FindByIdAsync("foo");

            user.Should().BeNull();
        }

        [Test]
        public async Task FindById_when_anonymous_id_cookie_is_exists_should_return_user()
        {
            _mockAnonymousUserFactory.Setup(m => m.CreateAsync(It.IsAny<string>())).ReturnsAsync(new AnonymousUser() { Id = "test" });
        
            var cookies = new Dictionary<string, string>()
            {
                { _anonOptions.CheckAnonymousIdCookieName, "test" }
            };
            _mockHttpContextAccessor.Object.HttpContext.Request.Cookies = new RequestCookieCollection(cookies);

            var user = await _subject.FindByIdAsync("test");

            user.Id.Should().Be("test");
        }

        [Test]
        public async Task FindById_if_no_anonymous_id_cookie_and_anonymous_id_from_session_is_exists_should_return_user()
        {
            _mockAnonymousUserFactory.Setup(m => m.CreateAsync(It.IsAny<string>())).ReturnsAsync(new AnonymousUser() { Id = "test" });
            _mockSharedUserSession.Setup(m => m.GetAnonymousIdAsync()).ReturnsAsync("test");

            var user = await _subject.FindByIdAsync("test");

            user.Id.Should().Be("test");
        }

        [Test]
        public async Task FindById_if_no_anonymous_id_cookie_and_anonymous_id_from_session_is_exists_should_create_anonymous_id_cookie()
        {
            _mockAnonymousUserFactory.Setup(m => m.CreateAsync(It.IsAny<string>())).ReturnsAsync(new AnonymousUser() { Id = "test" });
            _mockSharedUserSession.Setup(m => m.GetAnonymousIdAsync()).ReturnsAsync("test");

            var user = await _subject.FindByIdAsync("test");

            var cookieContainer = new CookieContainer();
            var cookies = _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Where(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value);
            cookieContainer.SetCookies(new Uri("http://server"), string.Join(",", cookies));
            _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Clear();

            var cookie = cookieContainer.GetCookies(new Uri("http://server")).Cast<Cookie>().Where(x => x.Name == _anonOptions.CheckAnonymousIdCookieName).FirstOrDefault();

            cookie.Value.Should().Be("test");
            user.Id.Should().Be("test");
        }
            
        #endregion
    
        #region DeleteById tests

        [Test]
        public async Task DeleteById_should_delete_cookie()
        {
            _mockAnonymousUserFactory.Setup(m => m.CreateAsync(It.IsAny<string>())).ReturnsAsync(new AnonymousUser() { Id = "test" });

            var reqCookies = new Dictionary<string, string>()
            {
                { _anonOptions.CheckAnonymousIdCookieName, "test" }
            };
            _mockHttpContextAccessor.Object.HttpContext.Request.Cookies = new RequestCookieCollection(reqCookies);

            await _subject.CreateAsync();

            await _subject.DeleteByIdAsync("test");

            var cookieContainer = new CookieContainer();
            var cookies = _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Where(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value);
            cookieContainer.SetCookies(new Uri("http://server"), string.Join(",", cookies));
            _mockHttpContextAccessor.Object.HttpContext.Response.Headers.Clear();

            var cookie = cookieContainer.GetCookies(new Uri("http://server")).Cast<Cookie>().Where(x => x.Name == _anonOptions.CheckAnonymousIdCookieName).FirstOrDefault();

            cookie.Should().BeNull();
        }
            
        #endregion
    }
}