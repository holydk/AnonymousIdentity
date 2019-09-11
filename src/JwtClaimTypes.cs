namespace AnonymousIdentity
{
    /// <summary>
    /// Commonly used claim types.
    /// </summary>
    public static class JwtClaimTypes
    {
        /// <summary>
        /// Unique Identifier for the anonymous End-User at the Issuer.
        /// </summary>
        public const string AnonymousId = "aid";

        /// <summary>
        /// Shared session identifier between anonymous user and real authenticated user.
        /// This represents a Session of an OP at an RP to a User Agent
        /// or device for a logged-in End-User. Its contents are unique to the OP and opaque
        /// to the RP.
        /// </summary>
        public const string SharedSessionId = "ssid";
    }
}