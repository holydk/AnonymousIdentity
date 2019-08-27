using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AnonymousIdentity.Configuration;
using AnonymousIdentity.Configuration.DependencyInjection;
using AnonymousIdentity.Services;
using AnonymousIdentity.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using IdentityServer4.Extensions;

namespace AnonymousIdentity.Hosting
{
    /// <summary>
    /// this decorates the real authentication service to detect when the
    /// user is being signed in.
    /// </summary>
    internal class AnonymousIdentityAuthenticationService : IAuthenticationService
    {
        #region Fields

        private readonly IAuthenticationService _inner;
        private readonly IAuthenticationSchemeProvider _schemes;
        private readonly ISharedUserSession _session;
        private readonly AnonymousIdentityServerOptions _options;
        private readonly IAnonymousUserManager _anonUserManager;

        #endregion

        #region Ctor

        public AnonymousIdentityAuthenticationService(
            Decorator<IAuthenticationService> decorator,
            IAuthenticationSchemeProvider schemes,
            ISharedUserSession session,
            AnonymousIdentityServerOptions options,
            IAnonymousUserManager anonUserManager
        )
        {
            _inner = decorator.Instance;
            _schemes = schemes;
            _session = session;
            _options = options;
            _anonUserManager = anonUserManager;
        }

        #endregion

        #region Methods

        public async Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties)
        {
            var defaultScheme = await _schemes.GetDefaultSignInSchemeAsync();
            var cookieScheme = await GetCookieAuthenticationSchemeAsync();

            if ((scheme == null && defaultScheme?.Name == cookieScheme) || scheme == cookieScheme)
            {
                if (principal?.IsAnonymous() == false)
                {
                    var currentPrincipal = await _session.GetUserAsync();
                    if (currentPrincipal?.IsAnonymous() == true)
                    {
                        await _anonUserManager.DeleteByIdAsync(currentPrincipal.GetSubjectId());
                    }
                }

                if (properties == null) properties = new AuthenticationProperties();
                await _session.CreateSessionIdAsync(principal, properties);
            }

            await _inner.SignInAsync(context, scheme, principal, properties);
        }

        public async Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties)
        {
            var defaultScheme = await _schemes.GetDefaultSignOutSchemeAsync();
            var cookieScheme = await GetCookieAuthenticationSchemeAsync();

            if ((scheme == null && defaultScheme?.Name == cookieScheme) || scheme == cookieScheme)
            {              
                await _session.RemoveSessionIdCookieAsync();
            }

            await _inner.SignOutAsync(context, scheme, properties);
        }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme)
        {
            return _inner.AuthenticateAsync(context, scheme);
        }

        public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties)
        {
            return _inner.ChallengeAsync(context, scheme, properties);
        }

        public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties)
        {
            return _inner.ForbidAsync(context, scheme, properties);
        }

        #endregion

        #region Utilities

        // todo: remove this in 3.0 and use extension method on http context
        private async Task<string> GetCookieAuthenticationSchemeAsync()
        {
            if (_options.CookieAuthenticationScheme != null)
            {
                return _options.CookieAuthenticationScheme;
            }

            var scheme = await _schemes.GetDefaultAuthenticateSchemeAsync();
            if (scheme == null)
            {
                throw new InvalidOperationException("No DefaultAuthenticateScheme found.");
            }
            return scheme.Name;
        }
            
        #endregion
    }
}