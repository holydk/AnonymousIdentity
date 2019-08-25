using System;
using System.Collections.Specialized;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Anonymous.Configuration.DependencyInjection;
using IdentityServer4.Validation;

namespace IdentityServer4.Anonymous.Validation
{
    /// <summary>
    /// this decorates the real authorize request validator 
    /// to process the anonymous authentication request.
    /// </summary>
    internal class AnonymousAuthorizeRequestValidator : IAuthorizeRequestValidator
    {
        #region Fields

        private readonly IAuthorizeRequestValidator _inner;

        #endregion

        #region Ctor

        public AnonymousAuthorizeRequestValidator(Decorator<IAuthorizeRequestValidator> decorator)
        {
            _inner = decorator.Instance;
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

                // check the request and if valid, return "json" response mode back
                var result = await _inner.ValidateAsync(parameters, subject);
                if (!result.IsError)
                {
                    result.ValidatedRequest.ResponseMode = OidcConstants.ResponseModes.Json;
                }

                return result;
            }

            return await _inner.ValidateAsync(parameters, subject);
        }
            
        #endregion
    }
}