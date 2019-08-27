using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace AnonymousIdentity.Services
{
    /// <summary>
    /// Models a shared session between anonymous user and real authenticated user.
    /// </summary>
    public interface ISharedUserSession
    {
        /// <summary>
        /// Gets the current authenticated user.
        /// </summary>
        Task<ClaimsPrincipal> GetUserAsync();

        /// <summary>
        /// Creates a session identifier for the signin context and issues the session id cookie.
        /// </summary>
        Task CreateSessionIdAsync(ClaimsPrincipal principal, AuthenticationProperties properties);

        /// <summary>
        /// Gets the current session identifier.
        /// </summary>
        /// <returns></returns>
        Task<string> GetSessionIdAsync();

        /// <summary>
        /// Ensures the session identifier cookie asynchronous.
        /// </summary>
        /// <returns></returns>
        Task EnsureSessionIdCookieAsync();

        /// <summary>
        /// Removes the session identifier cookie.
        /// </summary>
        Task RemoveSessionIdCookieAsync();

        /// <summary>
        /// Gets the anonymous identifier of the authenticated user (when he is not signed in).
        /// </summary>
        /// <returns></returns>
        Task<string> GetAnonymousIdAsync();
    }
}