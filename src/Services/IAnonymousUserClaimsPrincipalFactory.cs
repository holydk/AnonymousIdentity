using System.Security.Claims;
using System.Threading.Tasks;

namespace AnonymousIdentity.Services
{
    /// <summary>
    /// Provides an abstraction for a factory to create a System.Security.Claims.ClaimsPrincipal from a anonymous user.
    /// </summary>
    public interface IAnonymousUserClaimsPrincipalFactory
    {
        /// <summary>
        /// Creates a System.Security.Claims.ClaimsPrincipal from an anonymous user asynchronously.
        /// </summary>
        /// <param name="user">The user to create a System.Security.Claims.ClaimsPrincipal from.</param>
        /// <returns>The anonymous System.Security.Claims.ClaimsPrincipal.</returns>
        Task<ClaimsPrincipal> CreateAsync(IAnonymousUser user);
    }
}