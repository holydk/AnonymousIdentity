using System.Threading.Tasks;
using IdentityServer4.Anonymous.Configuration;
using IdentityServer4.Anonymous.Endpoints.Results;
using IdentityServer4.Anonymous.Services;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;

namespace IdentityServer4.Anonymous.Endpoints
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
        private readonly IAnonymousUserManager _anonUserManager;
        private readonly IAnonymousSignInManager _anonSignInManager;
        private readonly IAuthorizeRequestValidator _validator;
        private readonly IEndpointHandlerProvider _handlerProvider;
        private readonly IAuthorizeResponseGenerator _authorizeResponseGenerator;
        private readonly IAnonymousUserFactory _anonUserFactory;
        private readonly AnonymousIdentityServerOptions _anonIdsrvOptions;

        #endregion

        #region Ctor

        public AnonymousAuthorizeEndpoint(
            ISharedUserSession userSession,
            IAnonymousUserManager anonUserManager,
            IAnonymousSignInManager anonSignInManager,
            IAuthorizeRequestValidator validator,
            IEndpointHandlerProvider handlerProvider,
            IAuthorizeResponseGenerator authorizeResponseGenerator,
            IAnonymousUserFactory anonUserFactory,
            AnonymousIdentityServerOptions anonIdsrvOptions
        )
        {
            _userSession = userSession;
            _anonUserManager = anonUserManager;
            _anonSignInManager = anonSignInManager;
            _validator = validator;
            _handlerProvider = handlerProvider;
            _authorizeResponseGenerator = authorizeResponseGenerator;
            _anonUserFactory = anonUserFactory;
            _anonIdsrvOptions = anonIdsrvOptions;
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
                        if (user == null)
                        {
                            // create anon user
                            var anonUser = await _anonUserFactory.CreateAsync();
                            await _anonUserManager.CreateAsync(anonUser);
                            
                            // and sign in with "anon" authentication method
                            await _anonSignInManager.SignInAsync(anonUser);
                            
                            // reload the current user
                            request.Subject = await _userSession.GetUserAsync();
                        }

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