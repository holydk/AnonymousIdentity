using System;
using System.Collections.Specialized;
using System.Security.Claims;
using System.Threading.Tasks;
using AnonymousIdentity.Configuration;
using AnonymousIdentity.Extensions;
using AnonymousIdentity.Infrastructure.IdentityServer4.Configuration.DependencyInjection;
using AnonymousIdentity.Services;
using IdentityServer4.Validation;

namespace AnonymousIdentity.Infrastructure.IdentityServer4.Validation
{
    /// <summary>
    /// this decorates the real authorize request validator 
    /// to process the anonymous authentication request.
    /// </summary>
    internal class AnonymousAuthorizeRequestValidator : IAuthorizeRequestValidator
    {
        #region Fields

        private readonly IAuthorizeRequestValidator _inner;
        private readonly AnonymousIdentityServerOptions _options;
        private readonly ISharedUserSession _userSession;
        private readonly IAnonymousUserManager _anonUserManager;
        private readonly IAnonymousSignInManager _anonSignInManager;
        private readonly IAnonymousUserFactory _anonUserFactory;

        #endregion

        #region Ctor

        public AnonymousAuthorizeRequestValidator(
            Decorator<IAuthorizeRequestValidator> decorator,
            AnonymousIdentityServerOptions options,
            ISharedUserSession userSession,
            IAnonymousUserManager anonUserManager,
            IAnonymousSignInManager anonSignInManager,
            IAnonymousUserFactory anonUserFactory)
        {
            _inner = decorator.Instance;
            _options = options;
            _userSession = userSession;
            _anonUserManager = anonUserManager;
            _anonSignInManager = anonSignInManager;
            _anonUserFactory = anonUserFactory;
        }

        #endregion

        #region Methods

        /// <summary>
        ///  Validates authorize request parameters.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="subject"></param>
        /// <returns></returns>
        public async Task<AuthorizeRequestValidationResult> ValidateAsync(NameValueCollection parameters, ClaimsPrincipal subject = null)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            
            var responseMode = parameters.Get(IdentityModel.OidcConstants.AuthorizeRequest.ResponseMode);
            var acrValues = parameters.Get(IdentityModel.OidcConstants.AuthorizeRequest.AcrValues);
            if (acrValues == Constants.KnownAcrValues.Anonymous && responseMode == OidcConstants.ResponseModes.Json)
            {
                // source validator dont support "json" response mode
                // the "json" response mode only for anonymous requests
                parameters.Remove(IdentityModel.OidcConstants.AuthorizeRequest.ResponseMode);

                var result = await _inner.ValidateAsync(parameters, subject);
                if (!result.IsError)
                {
                    if (subject == null)
                    {
                        // create anon user
                        var anonUser = await _anonUserFactory.CreateAsync();
                        await _anonUserManager.CreateAsync(anonUser);
                        
                        // and sign in with "anon" authentication method
                        await _anonSignInManager.SignInAsync(anonUser);
                        
                        // reload the current user
                        result.ValidatedRequest.Subject = await _userSession.GetUserAsync();
                    }

                    // return "json" response mode back
                    result.ValidatedRequest.ResponseMode = OidcConstants.ResponseModes.Json;

                    // set anonymous token lifetime
                    // https://github.com/IdentityServer/IdentityServer4/issues/3578
                    if (result.ValidatedRequest.Subject.IsAnonymous())
                    {
                        result.ValidatedRequest.AccessTokenLifetime = _options.AccessTokenLifetime;
                    }
                }

                return result;
            }

            return await _inner.ValidateAsync(parameters, subject);
        }
            
        #endregion
    }
}