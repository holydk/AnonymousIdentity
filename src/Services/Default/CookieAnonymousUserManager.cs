using System;
using System.Threading.Tasks;
using AnonymousIdentity.Configuration;
using AnonymousIdentity.Extensions;
using Microsoft.AspNetCore.Http;

namespace AnonymousIdentity.Services
{
    /// <summary>
    /// Provides a default cookie based implementation the APIs for managing anonymous user.
    /// </summary>
    public class CookieAnonymousUserManager : IAnonymousUserManager
    {
        #region Fields

        private readonly string _checkAnonymousIdCookieName;
        private readonly HttpContext _httpContext;
        private readonly IAnonymousUserFactory _anonUserFactory;
        private readonly ISharedUserSession _sharedUserSession;

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new instance of <see cref="CookieAnonymousUserManager"/>.
        /// </summary>
        /// <param name="httpContextAccessor">The http context accessor.</param>
        /// <param name="anonUserFactory">The anonymous user factory.</param>
        /// <param name="sharedUserSession">The shared session.</param>
        /// <param name="anonOptions">The anonymous options.</param>
        public CookieAnonymousUserManager(
            IHttpContextAccessor httpContextAccessor,
            IAnonymousUserFactory anonUserFactory,
            ISharedUserSession sharedUserSession,
            AnonymousIdentityServerOptions anonOptions)
        {
            _httpContext = httpContextAccessor.HttpContext;
            _anonUserFactory = anonUserFactory;
            _sharedUserSession = sharedUserSession;
            _checkAnonymousIdCookieName = anonOptions.CheckAnonymousIdCookieName;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a anonymous user.
        /// </summary>
        /// <param name="user">The anonymous user.</param>
        /// <returns></returns>
        public async virtual Task CreateAsync(IAnonymousUser user = null)
        {
            user = user ?? await _anonUserFactory.CreateAsync();
            
            if (user == null) throw new InvalidOperationException(nameof(user));

            DeleteAnonymousIdCookie();
            AppendAnonymousIdCookie(user.Id);
        }

        /// <summary>
        /// Finds and returns a user if any, who has the specified <paramref name="userId"/>.
        /// </summary>
        /// <param name="userId">The user ID to search for.</param>
        /// <returns></returns>
        public async virtual Task<IAnonymousUser> FindByIdAsync(string userId)
        {
            if (userId == null) throw new ArgumentNullException(nameof(userId));

            if (!TryGetAnonymousIdCookie(out var sub))
            {
                var anonId = await _sharedUserSession.GetAnonymousIdAsync();
                if (anonId != null)
                {
                    AppendAnonymousIdCookie(anonId);

                    sub = anonId;
                }
            }

            if (sub == userId)
            {
                return await _anonUserFactory.CreateAsync(sub);
            }

            return null;
        }

        /// <summary>
        /// Deletes the specified anonymous <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The anonymous user.</param>
        /// <returns></returns>
        public Task DeleteAsync(IAnonymousUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            return DeleteByIdAsync(user.Id);
        }

        /// <summary>
        /// Deletes the specified anonymous user by id.
        /// </summary>
        /// <param name="userId">The user ID to delete for.</param>
        /// <returns></returns>
        public Task DeleteByIdAsync(string userId)
        {
            if (userId == null) throw new ArgumentNullException(nameof(userId));

            if (TryGetAnonymousIdCookie(out var sub) && sub == userId)
            {
                DeleteAnonymousIdCookie();
            }

            return Task.CompletedTask;
        }
            
        #endregion

        #region Utilities
            
        private bool TryGetAnonymousIdCookie(out string sub)
        {
            sub = null;
            if (_httpContext.Request.Cookies.TryGetValue(_checkAnonymousIdCookieName, out var value))
            {
                sub = value;
            }

            return sub != null;
        }

        private void AppendAnonymousIdCookie(string id)
        {
            if (!id.IsPresent()) throw new ArgumentNullException(nameof(id));

            _httpContext.Response.Cookies.Append(_checkAnonymousIdCookieName, id);
        }

        private void DeleteAnonymousIdCookie()
        {
            _httpContext.Response.Cookies.Delete(_checkAnonymousIdCookieName);
        }

        #endregion
    }
}