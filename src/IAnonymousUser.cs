namespace IdentityServer4.Anonymous
{
    /// <summary>
    /// Represents a anonymous user.
    /// </summary>
    public interface IAnonymousUser
    {
        /// <summary>
        /// Gets or sets the id for this anonymous user.
        /// </summary>
        string Id { get; set; }
    }
}