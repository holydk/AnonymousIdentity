using System.Threading.Tasks;

namespace AnonymousIdentity.Services
{
    /// <summary>
    /// Provides the APIs for managing anonymous user.
    /// </summary>
    public interface IAnonymousUserManager
    {
        /// <summary>
        /// Creates a anonymous user.
        /// </summary>
        /// <param name="user">The anonymous user.</param>
        /// <returns></returns>
        Task CreateAsync(IAnonymousUser user = null);

        /// <summary>
        /// Finds and returns a user if any, who has the specified <paramref name="userId"/>.
        /// </summary>
        /// <param name="userId">The user ID to search for.</param>
        /// <returns></returns>
        Task<IAnonymousUser> FindByIdAsync(string userId);

        /// <summary>
        /// Deletes the specified anonymous <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The anonymous user.</param>
        /// <returns></returns>
        Task DeleteAsync(IAnonymousUser user);

        /// <summary>
        /// Deletes the specified anonymous user by id.
        /// </summary>
        /// <param name="userId">The user ID to delete for.</param>
        /// <returns></returns>
        Task DeleteByIdAsync(string userId);
    }
}