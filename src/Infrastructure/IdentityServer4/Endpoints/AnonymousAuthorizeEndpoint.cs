using System.Threading.Tasks;
using AnonymousIdentity.Extensions;
using AnonymousIdentity.Infrastructure.IdentityServer4.Endpoints.Results;
using AnonymousIdentity.Infrastructure.IdentityServer4.Services;
using AnonymousIdentity.Services;
using IdentityServer4.Hosting;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;

namespace AnonymousIdentity.Infrastructure.IdentityServer4.Endpoints
{
    /// <summary>
    /// this decorates the real AuthorizeEndpoint that is internal class
    /// to detect the anonymous authentication request and issue an anonymous token
    /// https://github.com/IdentityServer/IdentityServer3/issues/1953#issuecomment-292584505
    /// </summary>
    internal class AnonymousAuthorizeEndpoint : IEndpointHandler
    {
        #region Fields

        private readonly ISharedUserSession _userSession;
        private readonly IAuthorizeRequestValidator _validator;
        private readonly IEndpointHandlerProvider _handlerProvider;
        private readonly IAuthorizeResponseGenerator _authorizeResponseGenerator;

        #endregion

        #region Ctor

        public AnonymousAuthorizeEndpoint(
            ISharedUserSession userSession,
            IAuthorizeRequestValidator validator,
            IEndpointHandlerProvider handlerProvider,
            IAuthorizeResponseGenerator authorizeResponseGenerator
        )
        {
            _userSession = userSession;
            _validator = validator;
            _handlerProvider = handlerProvider;
            _authorizeResponseGenerator = authorizeResponseGenerator;
        }
            
        #endregion

        #region Methods

        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            if (HttpMethods.IsGet(context.Request.Method))
            {
                var user = await _userSession.GetUserAsync();
                var parameters = context.Request.Query.AsNameValueCollection();
                var result = await _validator.ValidateAsync(parameters, user);
                if (!result.IsError)
                {
                    var request = result.ValidatedRequest;           
                    if (request.IsAnonymous() && request.ResponseMode == OidcConstants.ResponseModes.Json)
                    {
                        var response = await _authorizeResponseGenerator.CreateResponseAsync(request);

                        return new AuthorizeResult(response);
                    }
                }
            }
            
            // call source handler
            var innerHandler = _handlerProvider.GetByPath("/connect/authorize");
            return await innerHandler.ProcessAsync(context);
        }
            
        #endregion
    }
}