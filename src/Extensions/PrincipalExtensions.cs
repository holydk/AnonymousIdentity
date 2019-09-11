using System;
using System.Security.Claims;
using System.Security.Principal;

namespace AnonymousIdentity.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="System.Security.Principal.IPrincipal"/>.
    /// </summary>
    public static class PrincipalExtensions
    {
        /// <summary>
        /// Check if <see cref="IPrincipal"/> is anonymous.
        /// </summary>
        /// <param name="principal">The principal.</param>
        /// <returns>Return true if principal contains "anon" authentication method(amr) claim; otherwise false.</returns>
        public static bool IsAnonymous(this IPrincipal principal)
        {
            if (principal == null) throw new ArgumentNullException(nameof(principal));

            var id = principal as ClaimsPrincipal;
            var amr = id?.FindFirst(IdentityModel.JwtClaimTypes.AuthenticationMethod);

            return amr?.Value == OidcConstants.AuthenticationMethods.Anonymous;
        }
    }
}