using System;
using System.Security.Claims;
using System.Security.Principal;
using IdentityServer4.Anonymous;

namespace IdentityServer4.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="System.Security.Principal.IPrincipal"/>.
    /// </summary>
    public static class PrincipalExtensions
    {
        public static bool IsAnonymous(this IPrincipal principal)
        {
            if (principal == null) throw new ArgumentNullException(nameof(principal));

            var id = principal as ClaimsPrincipal;
            var amr = id?.FindFirst(IdentityModel.JwtClaimTypes.AuthenticationMethod);

            return amr?.Value == OidcConstants.AuthenticationMethods.Anonymous;
        }
    }
}