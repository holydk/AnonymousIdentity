using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("IdentityServer4.Anonymous.UnitTests")]
[assembly: InternalsVisibleTo("IdentityServer4.Anonymous.IntegrationTests")]
namespace IdentityServer4.Anonymous
{
    internal static class Constants
    {
        public const int DefaultTokenLifetime = 2592000;

        public static class KnownAcrValues
        {
            public const string Anonymous = "0";
        }
    }
}