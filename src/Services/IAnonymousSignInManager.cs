using System.Threading.Tasks;

namespace IdentityServer4.Anonymous.Services
{
    /// <summary>
    /// Provides the APIs for anonymous user sign in.
    /// </summary>
    public interface IAnonymousSignInManager
    {
        /// <summary>
        /// Signs in the specified anonymous user.
        /// </summary>
        /// <param name="user">The anonymous user.</param>
        /// <returns></returns>
        Task SignInAsync(IAnonymousUser user);
    }
}