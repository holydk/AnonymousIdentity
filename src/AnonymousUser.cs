namespace IdentityServer4.Anonymous
{
    /// <summary>
    /// Represents a base implementation of anonymous user.
    /// </summary>
    public class AnonymousUser : IAnonymousUser
    {
        /// <summary>
        /// Gets or sets the id for this anonymous user.
        /// </summary>
        public string Id { get; set; }
    }
}