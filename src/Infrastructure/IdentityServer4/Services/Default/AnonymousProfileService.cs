using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AnonymousIdentity.Configuration;
using AnonymousIdentity.Extensions;
using AnonymousIdentity.Infrastructure.IdentityServer4.Configuration.DependencyInjection;
using AnonymousIdentity.Services;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;

namespace AnonymousIdentity.Infrastructure.IdentityServer4.Services
{
    /// <summary>
    /// this decorates the real IProfileService to set anonymous claims.
    /// </summary>
    internal class AnonymousProfileService : IProfileService
    {
        #region Fields

        private readonly IProfileService _inner;
        private readonly ISharedUserSession _sharedUserSession;
        private readonly AnonymousIdentityServerOptions _anonIdsrvOptions;
        private readonly IAnonymousUserClaimsPrincipalFactory _anonPrincipalFactory;
        private readonly IAnonymousUserManager _anonUserManager;

        #endregion

        #region Ctor

        public AnonymousProfileService(
            Decorator<IProfileService> decorator,
            ISharedUserSession sharedUserSession,
            AnonymousIdentityServerOptions anonIdsrvOptions,
            IAnonymousUserClaimsPrincipalFactory anonPrincipalFactory,
            IAnonymousUserManager anonUserManager)
        {
            _inner = decorator.Instance;
            _sharedUserSession = sharedUserSession;
            _anonIdsrvOptions = anonIdsrvOptions;
            _anonPrincipalFactory = anonPrincipalFactory;
            _anonUserManager = anonUserManager;
        }

        #endregion

        #region Methods

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.Subject?.IsAnonymous() == true)
            {
                var sub = context.Subject.GetSubjectId();
                if (sub == null) throw new InvalidOperationException("No sub claim present");

                var anonUser = await _anonUserManager.FindByIdAsync(sub);
                if (anonUser != null)
                {
                    var principal = await _anonPrincipalFactory.CreateAsync(anonUser);
                    if (principal == null) throw new InvalidOperationException("AnonymousClaimsFactory failed to create a principal");

                    context.AddRequestedClaims(principal.Claims);
                }
            } 
            else if (context.Subject != null)
            {
                Claim aid = null;

                var anonId = await _sharedUserSession.GetAnonymousIdAsync();
                if (anonId != null)
                {
                    aid = new Claim(JwtClaimTypes.AnonymousId, anonId);
                }
                else
                {
                    var identity = context.Subject.Identities.First();
                    if (identity.HasClaim(x => x.Type == JwtClaimTypes.AnonymousId))
                    {
                        aid = identity.FindFirst(x => x.Type == JwtClaimTypes.AnonymousId);
                    }
                } 

                if (aid != null)
                {
                    if (_anonIdsrvOptions.AlwaysIncludeAnonymousIdInProfile)
                    {
                        context.IssuedClaims.Add(aid);
                    }
                    else
                    {
                        context.AddRequestedClaims(new[] { aid });
                    } 
                }          
            } 

            await _inner.GetProfileDataAsync(context);
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.Subject?.IsAnonymous() == true)
            {
                var sub = context.Subject.GetSubjectId();
                if (sub == null) throw new Exception("No subject Id claim present");

                var anonUser = await _anonUserManager.FindByIdAsync(sub);

                context.IsActive = anonUser != null;

                return;
            }

            await _inner.IsActiveAsync(context);
        }
            
        #endregion
    }
}