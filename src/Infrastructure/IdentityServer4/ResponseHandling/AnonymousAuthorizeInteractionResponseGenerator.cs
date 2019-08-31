using System.Threading.Tasks;
using AnonymousIdentity.Extensions;
using AnonymousIdentity.Infrastructure.IdentityServer4.Configuration.DependencyInjection;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Validation;

namespace AnonymousIdentity.Infrastructure.IdentityServer4.ResponseHandling
{
    /// <summary>
    /// this decorates the real IAuthorizeInteractionResponseGenerator and handles the case 
    /// when the user must login, but anonymous user is already signed in.
    /// </summary>
    internal class AnonymousAuthorizeInteractionResponseGenerator : IAuthorizeInteractionResponseGenerator
    {
        #region Fields

        private readonly IAuthorizeInteractionResponseGenerator _inner;

        #endregion

        #region Ctor

        public AnonymousAuthorizeInteractionResponseGenerator(Decorator<IAuthorizeInteractionResponseGenerator> decorator)
        {
            _inner = decorator.Instance;
        }

        #endregion

        #region Methods

        public Task<InteractionResponse> ProcessInteractionAsync(ValidatedAuthorizeRequest request, ConsentResponse consent = null)
        {
            if (request.Subject?.IsAnonymous() == true)
            {
                return Task.FromResult(new InteractionResponse { IsLogin = true });
            }

            return _inner.ProcessInteractionAsync(request, consent);
        }
            
        #endregion
    }
}