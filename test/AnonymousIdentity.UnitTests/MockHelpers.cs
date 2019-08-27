using System;
using System.Security.Claims;
using AnonymousIdentity.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AnonymousIdentity.UnitTests
{
    internal static class MockHelpers
    {
        internal const string DefaultAuthenticationSchemeName = "scheme";
        internal static AuthenticationScheme DefaultAuthenticationScheme = 
            new AuthenticationScheme(DefaultAuthenticationSchemeName, null, CreateMockAuthenticationHandler().Object.GetType());

        public static Mock<IHttpContextAccessor> CreateMockHttpContextAccessor(IServiceCollection services = null)
        {
            var mock = new Mock<IHttpContextAccessor>();
            var context = new DefaultHttpContext();
            services = services ?? new ServiceCollection();
            context.RequestServices = services.BuildServiceProvider();
            mock.SetupGet(m => m.HttpContext).Returns(() =>
            {
                return context;
            });

            return mock;
        }

        public static Mock<IAuthenticationSchemeProvider> CreateMockAuthenticationSchemeProvider(AuthenticationScheme scheme = null)
        {
            var mock = new Mock<IAuthenticationSchemeProvider>();
            mock.Setup(m => m.GetDefaultAuthenticateSchemeAsync()).ReturnsAsync(() =>
            {
                return scheme ?? DefaultAuthenticationScheme;
            });

            return mock;
        }

        public static Mock<IAuthenticationHandler> CreateMockAuthenticationHandler(AuthenticateResult result = null)
        {
            var mock = new Mock<IAuthenticationHandler>();
            mock.Setup(m => m.AuthenticateAsync()).ReturnsAsync(() =>
            {
                return result ?? AuthenticateResult.NoResult();
            });
            
            return mock;
        }

        public static Mock<IAuthenticationHandlerProvider> CreateMockAuthenticationHandlerProvider(IAuthenticationHandler handler = null)
        {
            var mock = new Mock<IAuthenticationHandlerProvider>();
            mock.Setup(m => m.GetHandlerAsync(It.IsAny<HttpContext>(), It.IsAny<string>())).ReturnsAsync(() => 
            {
                return handler ?? CreateMockAuthenticationHandler().Object;
            });

            return mock;
        }

        public static Mock<ISystemClock> CreateMockSystemClock(DateTime? dateTime = null)
        {
            var mock = new Mock<ISystemClock>();
            mock.SetupGet(m => m.UtcNow).Returns(() => 
            {
                return new DateTimeOffset(dateTime ?? DateTime.UtcNow);
            });

            return mock;
        }

        public static Mock<IAuthenticationService> CreateMockAuthenticationService(AuthenticateResult result = null)
        {
            var mock = new Mock<IAuthenticationService>();
            mock.Setup(m => m.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>())).ReturnsAsync(() => 
            {
                return result ?? AuthenticateResult.NoResult();
            });

            return mock;
        }

        public static Mock<IAnonymousUserFactory> CreateMockAnonymousUserFactory(IAnonymousUser user = null)
        {
            var mock = new Mock<IAnonymousUserFactory>();
            mock.Setup(m => m.CreateAsync(It.IsAny<string>())).ReturnsAsync(() => 
            {
                return user;
            });

            return mock;
        }

        public static Mock<ISharedUserSession> CreateMockSharedUserSession(ClaimsPrincipal user = null)
        {
            var mock = new Mock<ISharedUserSession>();
            mock.Setup(m => m.GetUserAsync()).ReturnsAsync(() => 
            {
                return user;
            });

            return mock;
        }
    }
}