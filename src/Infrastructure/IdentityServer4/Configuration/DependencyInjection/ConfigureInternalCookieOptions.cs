using AnonymousIdentity.Configuration;
using IdentityServer4.Configuration;
using Microsoft.Extensions.Options;

namespace AnonymousIdentity.Infrastructure.IdentityServer4.Configuration
{
    internal class PostConfigureInternalCookieOptions : IPostConfigureOptions<IdentityServerOptions>
    {
        #region Fields

        private readonly AnonymousIdentityServerOptions _anonOptions;

        #endregion

        #region Ctor

        public PostConfigureInternalCookieOptions(AnonymousIdentityServerOptions anonOptions)
        {
            _anonOptions = anonOptions;
        }

        #endregion

        #region Methods

        public void PostConfigure(string name, IdentityServerOptions options)
        {
            _anonOptions.CookieAuthenticationScheme = options.Authentication.CookieAuthenticationScheme;
        }
            
        #endregion
    }
}