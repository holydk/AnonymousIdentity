using System;
using System.Linq;
using System.Threading.Tasks;
using AnonymousIdentity.Extensions;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AnonymousIdentity.Infrastructure.IdentityServer4.Endpoints.Results
{
    internal class AuthorizeResult : IEndpointResult
    {
        #region Fields

        private readonly AuthorizeResponse _response;
        private IdentityServerOptions _options;
        private IMessageStore<ErrorMessage> _errorMessageStore;
        private ISystemClock _clock;

        #endregion

        #region Ctor

        public AuthorizeResult(AuthorizeResponse response)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
        }
            
        #endregion

        #region Methods

        public async Task ExecuteAsync(HttpContext context)
        {
            Init(context);

            if (_response.IsError)
            {
                await ProcessErrorAsync(context);
            }
            else
            {
                await RenderAuthorizeResponseAsync(context);
            }
        }
            
        #endregion

        #region Utilities

        private void Init(HttpContext context)
        {
            _options = _options ?? context.RequestServices.GetRequiredService<IdentityServerOptions>();
            _errorMessageStore = _errorMessageStore ?? context.RequestServices.GetRequiredService<IMessageStore<ErrorMessage>>();
            _clock = _clock ?? context.RequestServices.GetRequiredService<ISystemClock>();
        }

        private async Task ProcessErrorAsync(HttpContext context)
        {
            // these are the conditions where we can send a response 
            // back directly to the client, otherwise we're only showing the error UI
            var isPromptNoneError = _response.Error == IdentityModel.OidcConstants.AuthorizeErrors.AccountSelectionRequired ||
                _response.Error == IdentityModel.OidcConstants.AuthorizeErrors.LoginRequired ||
                _response.Error == IdentityModel.OidcConstants.AuthorizeErrors.ConsentRequired ||
                _response.Error == IdentityModel.OidcConstants.AuthorizeErrors.InteractionRequired;

            if (_response.Error == IdentityModel.OidcConstants.AuthorizeErrors.AccessDenied ||
                (isPromptNoneError && _response.Request?.PromptMode == IdentityModel.OidcConstants.PromptModes.None)
            )
            {
                // this scenario we can return back to the client
                await RenderAuthorizeResponseAsync(context);
            }
            else
            {
                // we now know we must show error page
                await RedirectToErrorPageAsync(context);
            }
        }

        private async Task RenderAuthorizeResponseAsync(HttpContext context)
        {
            if (_response.Request.ResponseMode == OidcConstants.ResponseModes.Json)
            {
                context.Response.SetNoCache();

                var result = _response.ToDictionary();
                await context.Response.WriteJsonAsync(result);
            }
            else
            {
                throw new InvalidOperationException("Unsupported response mode.");
            }
        }

        private async Task RedirectToErrorPageAsync(HttpContext context)
        {
            var errorModel = new ErrorMessage
            {
                RequestId = context.TraceIdentifier,
                Error = _response.Error,
                ErrorDescription = _response.ErrorDescription,
                UiLocales = _response.Request?.UiLocales,
                DisplayMode = _response.Request?.DisplayMode,
                ClientId = _response.Request?.ClientId
            };

            if (_response.Request?.ResponseMode != null)
            {
                errorModel.ResponseMode = _response.Request.ResponseMode;
            }

            var message = new Message<ErrorMessage>(errorModel, _clock.UtcNow.UtcDateTime);
            var id = await _errorMessageStore.WriteAsync(message);

            var errorUrl = _options.UserInteraction.ErrorUrl;

            var url = errorUrl.AddQueryString(_options.UserInteraction.ErrorIdParameter, id);
            context.Response.RedirectToAbsoluteUrl(url);
        }
            
        #endregion
    }
}