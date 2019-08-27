namespace AnonymousIdentity.Configuration
{
    /// <summary>
    /// The anonymous identity server options.
    /// </summary>
    public class AnonymousIdentityServerOptions
    {
        /// <summary>
        /// Gets or sets anonymous access token lifetime. (defaults to 2592000 seconds / 30 days)
        /// </summary>
        public int AccessTokenLifetime { get; set; } = Constants.DefaultTokenLifetime;

        /// <summary>
        /// Gets or sets anonymous identity token lifetime. (defaults to 2592000 seconds / 30 days)
        /// </summary>
        public int IdentityTokenLifetime { get; set; } = Constants.DefaultTokenLifetime;

        /// <summary>
        /// Gets or sets a value indicating whether the anonymous id is included in profile always.
        /// If true, the anonymous id will be included in profile always;
        /// otherwise will be included if requested with scope/resource.
        /// (claim "aid" should be contains in scope/resource). (defaults to true)
        /// </summary>
        /// <seealso cref="AnonymousIdentity.Services.IProfileService"/>
        public bool AlwaysIncludeAnonymousIdInProfile { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the shared session id is included in access token.
        /// If true, the share session id between anonymous user and "real" authenticated user 
        /// will be included in access token. (defaults to true)
        /// </summary>
        public bool IncludeSharedSessionIdInAccessToken { get; set; } = true;

        /// <summary>
        /// Gets or sets the name of the cookie used for the check shared session endpoint.
        /// </summary>
        /// <value></value>
        public string CheckSharedSessionCookieName { get; set; } = IdentityServerConstants.DefaultCheckSharedSessionCookieName;

        /// <summary>
        /// Gets or sets the name of the cookie used for the check anonymous subject endpoint.
        /// </summary>
        /// <value></value>
        public string CheckAnonymousIdCookieName { get; set; } = IdentityServerConstants.DefaultCheckAnonymousIdCookieName;
    
        /// <summary>
        /// Sets the cookie authenitcation scheme confgured by the host used for interactive users.
        /// If not set, the scheme will inferred from the host's default authentication scheme or identity server provider.
        /// This setting is typically used when AddPolicyScheme is used in the host as the default scheme.
        /// </summary>
        public string CookieAuthenticationScheme { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the check shared session endpoint is enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if the check shared session endpoint is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool EnableCheckSharedSessionEndpoint { get; set; } = true;
    }
}