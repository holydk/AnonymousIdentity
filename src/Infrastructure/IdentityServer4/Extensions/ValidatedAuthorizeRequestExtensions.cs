using System.Linq;
using AnonymousIdentity;

namespace IdentityServer4.Validation
{
    /// <summary>
    /// The extension methods for <see cref="ValidatedAuthorizeRequest"/>.
    /// </summary>
    public static class ValidatedAuthorizeRequestExtensions
    {
        /// <summary>
        /// Check if <see cref="ValidatedAuthorizeRequest"/> is anonymous.
        /// </summary>
        /// <param name="request">The validated authorize request.</param>
        /// <returns>Return true if authentication context reference classes contains "0"; otherwise false.</returns>
        public static bool IsAnonymous(this ValidatedAuthorizeRequest request)
        {
            return request.AuthenticationContextReferenceClasses.Count == 1
                     && request.AuthenticationContextReferenceClasses.First() == Constants.KnownAcrValues.Anonymous;
        }
    }
}