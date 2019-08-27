using System.Threading.Tasks;

namespace AnonymousIdentity.Services
{
    /// <summary>
    /// Provides an abstraction for a factory to create IAnonymousUser.
    /// </summary>
    public interface IAnonymousUserFactory
    {
        /// <summary>
        /// Creates a new anonymous user by id.
        /// </summary>
        /// <param name="subject">The id.</param>
        /// <returns>The anonymous user.</returns>
        Task<IAnonymousUser> CreateAsync(string subject = null);
    }
}