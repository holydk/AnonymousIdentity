using IdentityServer4.Hosting;

namespace AnonymousIdentity.Infrastructure.IdentityServer4.Services
{
    /// <summary>
    /// Represents a endpoint handlers factory.
    /// </summary>
    public interface IEndpointHandlerProvider
    {
        /// <summary>
        /// Gets original endpoint handler by path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The endpoint handler.</returns>
        IEndpointHandler GetByPath(string path);
    }
}