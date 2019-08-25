using System.Linq;
using IdentityServer4.Anonymous;

namespace IdentityServer4.Validation
{
    public static class ValidatedAuthorizeRequestExtensions
    {
        public static bool IsAnonymous(this ValidatedAuthorizeRequest request)
        {
            return request.AuthenticationContextReferenceClasses.Count == 1
                     && request.AuthenticationContextReferenceClasses.First() == Constants.KnownAcrValues.Anonymous;
        }
    }
}