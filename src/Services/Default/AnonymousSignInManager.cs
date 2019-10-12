using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using AnonymousIdentity.Configuration;

namespace AnonymousIdentity.Services
{
    /// <summary>
    /// Provides the APIs for anonymous user sign in.
    /// </summary>
    public class AnonymousSignInManager : IAnonymousSignInManager
    {
        #region Fields

        private readonly AnonymousIdentityServerOptions _options;
        private readonly IAuthenticationSchemeProvider _schemes;
        private readonly IAnonymousUserClaimsPrincipalFactory _anonPrincipalFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private HttpContext _httpContext => _httpContextAccessor.HttpContext;

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new instance of <see cref="AnonymousSignInManager"/>.
        /// </summary>
        /// <param name="options">The anonymous options.</param>
        /// <param name="httpContextAccessor">The http context accessor.</param>
        /// <param name="schemes">The schemes provider.</param>
        /// <param name="anonPrincipalFactory">The anonymous principal factory.</param>
        public AnonymousSignInManager(
            AnonymousIdentityServerOptions options,
            IHttpContextAccessor httpContextAccessor,
            IAuthenticationSchemeProvider schemes,
            IAnonymousUserClaimsPrincipalFactory anonPrincipalFactory)
        {
            _options = options;
            _httpContextAccessor = httpContextAccessor;
            _schemes = schemes;
            _anonPrincipalFactory = anonPrincipalFactory;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Signs in the specified anonymous user.
        /// </summary>
        /// <param name="user">The anonymous user.</param>
        /// <returns></returns>
        public virtual async Task SignInAsync(IAnonymousUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            
            var principal = await _anonPrincipalFactory.CreateAsync(user);

            // add anon authentication method
            principal.Identities.First()
                .AddClaim(new Claim(IdentityModel.JwtClaimTypes.AuthenticationMethod, OidcConstants.AuthenticationMethods.Anonymous));

            var scheme = await GetCookieSchemeAsync();
            
            await _httpContext.SignInAsync(scheme, principal);
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
            
        #endregion
    }
}