using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AnonymousIdentity.Configuration;
using Microsoft.AspNetCore.Authentication;

namespace AnonymousIdentity.Services
{
    /// <summary>
    /// Provides a default implementation a factory to create a System.Security.Claims.ClaimsPrincipal from a anonymous user.
    /// </summary>
    public class AnonymousUserClaimsPrincipalFactory : IAnonymousUserClaimsPrincipalFactory
    {
        #region Fields

        private readonly AnonymousIdentityServerOptions _options;
        private readonly IAuthenticationSchemeProvider _schemes;

        #endregion

        #region Ctor

        public AnonymousUserClaimsPrincipalFactory(
            AnonymousIdentityServerOptions options,
            IAuthenticationSchemeProvider schemes)
        {
            _options = options;
            _schemes = schemes;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a System.Security.Claims.ClaimsPrincipal from an anonymous user asynchronously.
        /// </summary>
        /// <param name="user">The user to create a System.Security.Claims.ClaimsPrincipal from.</param>
        /// <returns>The anonymous System.Security.Claims.ClaimsPrincipal.</returns>
        public async Task<ClaimsPrincipal> CreateAsync(IAnonymousUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            
            var id = await GenerateClaimsAsync(user);
            return new ClaimsPrincipal(id);
        }
            
        #endregion

        #region Utilities

        /// <summary>
        /// Generate the claims for a anonymous user.
        /// </summary>
        /// <param name="user">The anonymous user to create a <see cref="ClaimsIdentity"/> from.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous creation operation, containing the created <see cref="ClaimsIdentity"/>.</returns>
        protected virtual async Task<ClaimsIdentity> GenerateClaimsAsync(IAnonymousUser user)
        {
            var scheme = await GetCookieSchemeAsync();
            var id = new ClaimsIdentity(scheme);
            id.AddClaim(new Claim(IdentityModel.JwtClaimTypes.Subject, user.Id));

            return id;
        }

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