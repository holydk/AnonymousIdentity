using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("AnonymousIdentity.UnitTests")]
[assembly: InternalsVisibleTo("AnonymousIdentity.IntegrationTests")]
namespace AnonymousIdentity
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