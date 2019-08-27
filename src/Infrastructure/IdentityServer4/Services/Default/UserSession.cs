using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AnonymousIdentity.Infrastructure.IdentityServer4.Configuration.DependencyInjection;
using AnonymousIdentity.Services;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;

namespace AnonymousIdentity.Infrastructure.IdentityServer4.Services
{
    /// <summary>
    /// this decorates the real IUserSession to ensure what shared session cookie is issued.
    /// IdentityServerMiddleware runs IUserSession.EnsureSessionIdCookieAsync when start processing request.
    /// https://github.com/IdentityServer/IdentityServer4/blob/master/src/IdentityServer4/src/Hosting/IdentityServerMiddleware.cs
    /// </summary>
    internal class UserSession : IUserSession
    {
        #region Fields

        private readonly IUserSession _inner;
        private readonly ISharedUserSession _sharedSession;

        #endregion

        #region Ctor

        public UserSession(Decorator<IUserSession> decorator, ISharedUserSession sharedSession)
        {
            _inner = decorator.Instance;
            _sharedSession = sharedSession;
        }

        #endregion

        #region Methods

        public Task EnsureSessionIdCookieAsync()
        {
            _sharedSession.EnsureSessionIdCookieAsync();
            return _inner.EnsureSessionIdCookieAsync();
        }

        public Task AddClientIdAsync(string clientId)
        {
            return _inner.AddClientIdAsync(clientId);
        }

        public Task CreateSessionIdAsync(ClaimsPrincipal principal, AuthenticationProperties properties)
        {
            return _inner.CreateSessionIdAsync(principal, properties);
        }

        public Task<IEnumerable<string>> GetClientListAsync()
        {
            return _inner.GetClientListAsync();
        }

        public Task<string> GetSessionIdAsync()
        {
            return _inner.GetSessionIdAsync();
        }

        public Task<ClaimsPrincipal> GetUserAsync()
        {
            return _inner.GetUserAsync();
        }

        public Task RemoveSessionIdCookieAsync()
        {
            return _inner.RemoveSessionIdCookieAsync();
        }
            
        #endregion
    }
}