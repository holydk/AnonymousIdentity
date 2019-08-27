using System;
using System.Threading.Tasks;

namespace AnonymousIdentity.Services
{
    /// <summary>
    /// Provides the APIs to create IAnonymousUser.
    /// </summary>
    public class AnonymousUserFactory : IAnonymousUserFactory
    {
        #region Methods

        /// <summary>
        /// Creates a new anonymous user by id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>The anonymous user.</returns>
        public Task<IAnonymousUser> CreateAsync(string id = null)
        {
            var user = new AnonymousUser()
            {
                Id = id ?? Guid.NewGuid().ToString()
            };

            return Task.FromResult<IAnonymousUser>(user);
        }
            
        #endregion
    }
}