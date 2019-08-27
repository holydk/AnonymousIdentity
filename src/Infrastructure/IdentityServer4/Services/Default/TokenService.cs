using System.Security.Claims;
using System.Threading.Tasks;
using AnonymousIdentity.Configuration;
using AnonymousIdentity.Extensions;
using AnonymousIdentity.Infrastructure.IdentityServer4.Configuration.DependencyInjection;
using AnonymousIdentity.Services;
using IdentityServer4.Models;
using IdentityServer4.Services;

namespace AnonymousIdentity.Infrastructure.IdentityServer4.Services
{
    /// <summary>
    /// this decorates the real ITokenService to set anonymous tokens lifetime and shared session id.
    /// </summary>
    internal class TokenService : ITokenService
    {
        #region Fields

        private readonly ITokenService _inner;
        private readonly AnonymousIdentityServerOptions _anonIdsrvOptions;
        private readonly ISharedUserSession _sharedUserSession;

        #endregion

        #region Ctor

        public TokenService(
            Decorator<ITokenService> decorator,
            AnonymousIdentityServerOptions anonIdsrvOptions,
            ISharedUserSession sharedUserSession)
        {
            _inner = decorator.Instance;
            _anonIdsrvOptions = anonIdsrvOptions;
            _sharedUserSession = sharedUserSession;
        }

        #endregion

        #region Methods

        public async Task<Token> CreateAccessTokenAsync(TokenCreationRequest request)
        {
            var token = await _inner.CreateAccessTokenAsync(request);

            if (_anonIdsrvOptions.IncludeSharedSessionIdInAccessToken)
            {
                var ssid = await _sharedUserSession.GetSessionIdAsync();
                if (ssid != null)
                {
                    token.Claims.Add(new Claim(JwtClaimTypes.SharedSessionId, ssid));
                }
            }

            if (request?.Subject?.IsAnonymous() == true)
            {
                ChangeTokenLifetime(token, _anonIdsrvOptions.AccessTokenLifetime);
            }

            return token;
        }

        public async Task<Token> CreateIdentityTokenAsync(TokenCreationRequest request)
        {
            var token = await _inner.CreateIdentityTokenAsync(request);

            if (request?.Subject?.IsAnonymous() == true)
            {
                ChangeTokenLifetime(token, _anonIdsrvOptions.IdentityTokenLifetime);
            }

            return token;
        }

        public Task<string> CreateSecurityTokenAsync(Token token)
        {
            return _inner.CreateSecurityTokenAsync(token);
        }
            
        #endregion

        #region Utilities

        private void ChangeTokenLifetime(Token token, int lifetime)
        {
            if (token != null)
            {
                token.Lifetime = lifetime;
            }
        }
            
        #endregion
    }
}