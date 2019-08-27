using System;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using IdentityModel;
using AnonymousIdentity.Configuration;
using AnonymousIdentity.Infrastructure;
using AnonymousIdentity.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using IdentityServer4.Extensions;

namespace AnonymousIdentity.Services
{
    /// <summary>
    /// Cookie-based session implementation.
    /// This implementation create and share session between anonymous user and "real" authenticated user.
    /// This design requires this to be in DI as scoped.
    /// </summary>
    public class SharedUserSession : ISharedUserSession
    {
        #region Fields

        internal const string SessionIdKey = "shared_session_id";
        internal const string AnonymousIdKey = "anonymous_id";
        private readonly string _checkSessionCookieName;
        private readonly HttpContext _httpContext;
        private readonly IAuthenticationSchemeProvider _schemes;
        private readonly IAuthenticationHandlerProvider _handlers;
        private readonly AnonymousIdentityServerOptions _options;
        private readonly ISystemClock _clock;
        private ClaimsPrincipal _principal;
        private AuthenticationProperties _properties;

        #endregion

        #region Ctor

        public SharedUserSession(
            IHttpContextAccessor httpContextAccessor,
            IAuthenticationSchemeProvider schemes,
            IAuthenticationHandlerProvider handlers,
            AnonymousIdentityServerOptions options,
            ISystemClock clock)
        {
            _schemes = schemes;
            _handlers = handlers;
            _options = options;
            _clock = clock;
            _httpContext = httpContextAccessor.HttpContext;
            _checkSessionCookieName = _options.CheckSharedSessionCookieName;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the current authenticated user.
        /// </summary>
        public virtual async Task<ClaimsPrincipal> GetUserAsync()
        {
            await AuthenticateAsync();

            return _principal;
        }

        /// <summary>
        /// Creates a session identifier for the signin context and issues the session id cookie.
        /// </summary>
        public virtual async Task CreateSessionIdAsync(ClaimsPrincipal principal, AuthenticationProperties properties)
        {
            if (principal == null) throw new ArgumentNullException(nameof(principal));
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            var currentPrincipal = await GetUserAsync();
            var currentSubjectId = currentPrincipal?.GetSubjectId();
            var newSubjectId = principal.GetSubjectId();

            var needToSetAnonymousId = false;

            // issue to new user the current session id.
            if (currentPrincipal?.IsAnonymous() == true && currentSubjectId != newSubjectId)
            {
                var currentSid = await GetSessionIdAsync();
                if (currentSid != null)
                {
                    properties.Items[SessionIdKey] = currentSid;
                }

                if (!principal.IsAnonymous())
                {
                    needToSetAnonymousId = true;
                }
            }

            if (!properties.Items.ContainsKey(SessionIdKey) 
                  || (currentSubjectId != newSubjectId && currentPrincipal?.IsAnonymous() == false))
                       
            {
                properties.Items[SessionIdKey] = CryptoRandom.CreateUniqueId(16);
            }

            IssueSessionIdCookie(properties.Items[SessionIdKey]);

            _principal = principal;
            _properties = properties;

            if (needToSetAnonymousId)
            {
                await SetAnonumousIdAsync(currentSubjectId);
            }
        }

        /// <summary>
        /// Gets the current session identifier.
        /// </summary>
        /// /// <returns></returns>
        public virtual async Task<string> GetSessionIdAsync()
        {
            await AuthenticateAsync();

            if (_properties?.Items.ContainsKey(SessionIdKey) == true)
            {
                return _properties.Items[SessionIdKey];
            }

            return null;
        }

        /// <summary>
        /// Ensures the session identifier cookie asynchronous.
        /// </summary>
        /// <returns></returns>
        public virtual async Task EnsureSessionIdCookieAsync()
        {
            var sid = await GetSessionIdAsync();
            if (sid != null)
            {
                IssueSessionIdCookie(sid);
            }
            else
            {
                await RemoveSessionIdCookieAsync();
            }
        }

        /// <summary>
        /// Removes the session identifier cookie.
        /// </summary>
        public virtual Task RemoveSessionIdCookieAsync()
        {
            if (_httpContext.Request.Cookies.ContainsKey(_checkSessionCookieName))
            {
                // only remove it if we have it in the request
                var options = CreateSessionIdCookieOptions();
                options.Expires = _clock.UtcNow.UtcDateTime.AddYears(-1);

                _httpContext.Response.Cookies.Append(_checkSessionCookieName, ".", options);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the anonymous identifier of the authenticated user (when he is not signed in).
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetAnonymousIdAsync()
        {
            var encodedId = await GetAnonumousIdPropertyValueAsync();
            try
            {
                return DecodeAnonumousId(encodedId);
            }
            catch (Exception)
            {
                await SetAnonumousIdAsync(null);
            }

            return null;
        }
            
        #endregion

        #region Utilities

        // todo: remove this in 3.0 and use extension method on http context
        private async Task<string> GetCookieSchemeAsync()
        {
            if (_options.CookieAuthenticationScheme != null)
            {
                return _options.CookieAuthenticationScheme;
            }

            var defaultScheme = await _schemes.GetDefaultAuthenticateSchemeAsync();
            if (defaultScheme == null)
            {
                throw new InvalidOperationException("No DefaultAuthenticateScheme found.");
            }

            return defaultScheme.Name;
        }

        /// <summary>
        /// Authenticates the authentication cookie for the current HTTP request and caches the user and properties results.
        /// </summary>
        protected virtual async Task AuthenticateAsync()
        {
            if (_principal == null || _properties == null)
            {
                var scheme = await GetCookieSchemeAsync();

                var handler = await _handlers.GetHandlerAsync(_httpContext, scheme);
                if (handler == null)
                {
                    throw new InvalidOperationException($"No authentication handler is configured to authenticate for the scheme: {scheme}");
                }

                var result = await handler.AuthenticateAsync();
                if (result != null && result.Succeeded)
                {
                    _principal = result.Principal;
                    _properties = result.Properties;
                }
            }
        }

        /// <summary>
        /// Issues the cookie that contains the session id.
        /// </summary>
        /// <param name="sid"></param>
        protected virtual void IssueSessionIdCookie(string sid)
        {
            if (_options.EnableCheckSharedSessionEndpoint)
            {
                if (_httpContext.Request.Cookies[_checkSessionCookieName] != sid)
                {
                    _httpContext.Response.Cookies.Append(
                        _checkSessionCookieName,
                        sid,
                        CreateSessionIdCookieOptions());
                }
            }
        }

        /// <summary>
        /// Creates the options for the session cookie.
        /// </summary>
        protected virtual CookieOptions CreateSessionIdCookieOptions()
        {
            var secure = _httpContext.Request.IsHttps;
            var path = _httpContext.GetIdentityServerBasePath().CleanUrlPath();

            var options = new CookieOptions
            {
                HttpOnly = false,
                Secure = secure,
                Path = path,
                IsEssential = true,
                SameSite = SameSiteMode.None
            };

            return options;
        }

        private async Task SetAnonumousIdAsync(string id)
        {
            var encodedId = EncodeAnonumousId(id);
            await SetAnonumousIdPropertyValueAsync(encodedId);
        }

        private async Task SetAnonumousIdPropertyValueAsync(string value)
        {
            await AuthenticateAsync();

            if (_principal == null || _properties == null) throw new InvalidOperationException("User is not currently authenticated");

            if (value == null)
            {
                _properties.Items.Remove(AnonymousIdKey);
            }
            else
            {
                _properties.Items[AnonymousIdKey] = value;
            }

            var scheme = await GetCookieSchemeAsync();
            await _httpContext.SignInAsync(scheme, _principal, _properties);
        }

        private async Task<string> GetAnonumousIdPropertyValueAsync()
        {
            await AuthenticateAsync();

            if (_properties?.Items.ContainsKey(AnonymousIdKey) == true)
            {
                return _properties.Items[AnonymousIdKey];
            }

            return null;
        }

        private string DecodeAnonumousId(string encodedId)
        {
            if (encodedId.IsPresent())
            {
                var bytes = Base64Url.Decode(encodedId);
                encodedId = Encoding.UTF8.GetString(bytes);
                return ObjectSerializer.FromString<string>(encodedId);
            }

            return null;
        }

        private string EncodeAnonumousId(string id)
        {
            if (id.IsPresent())
            {
                var value = ObjectSerializer.ToString(id);
                var bytes = Encoding.UTF8.GetBytes(value);
                value = Base64Url.Encode(bytes);
                return value;
            }

            return null;
        }
            
        #endregion
    }
}