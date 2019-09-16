namespace KenticoKontent.Delivery
{
    /// <summary>
    /// Represents a filter that matches a content item of the given content type.
    /// </summary>
    public sealed class SystemTypeEqualsFilter : Filter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemTypeEqualsFilter"/> class.
        /// </summary>
        /// <param name="codename">Content type codename.</param>
        public SystemTypeEqualsFilter(string codename) : base("system.type", codename)
        {
        }
    }
}
