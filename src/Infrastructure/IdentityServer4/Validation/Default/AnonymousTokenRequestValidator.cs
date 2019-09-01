using System.Collections.Specialized;
using System.Threading.Tasks;
using AnonymousIdentity.Configuration;
using AnonymousIdentity.Extensions;
using AnonymousIdentity.Infrastructure.IdentityServer4.Configuration.DependencyInjection;
using IdentityServer4.Validation;

namespace AnonymousIdentity.Infrastructure.IdentityServer4.Validation
{
    /// <summary>
    /// this decorates the real token request validator 
    /// to setup anonymous token lifetime.
    /// https://github.com/IdentityServer/IdentityServer4/issues/3578
    /// </summary>
    internal class AnonymousTokenRequestValidator : ITokenRequestValidator
    {
        #region Fields

        private readonly ITokenRequestValidator _inner;
        private readonly AnonymousIdentityServerOptions _options;

        #endregion

        #region Ctor

        public AnonymousTokenRequestValidator(
            Decorator<ITokenRequestValidator> decorator,
            AnonymousIdentityServerOptions options)
        {
            _inner = decorator.Instance;
            _options = options;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Validates the request.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="clientValidationResult">The client validation result.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// parameters
        /// or
        /// client
        /// </exception>
        public async Task<TokenRequestValidationResult> ValidateRequestAsync(NameValueCollection parameters, ClientSecretValidationResult clientValidationResult)
        {
            var result = await _inner.ValidateRequestAsync(parameters, clientValidationResult);
            if (!result.IsError)
            {
                if (result.ValidatedRequest.Subject?.IsAnonymous() == true)
                {
                    result.ValidatedRequest.AccessTokenLifetime = _options.AccessTokenLifetime;
                }
            }
            
            return result;
        }
            
        #endregion
    }
}