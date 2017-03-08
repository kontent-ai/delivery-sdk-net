namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a context in which content links in Rich text element values are resolved.
    /// </summary>
    public sealed class ContentLinkUrlResolverContext
    {
        /// <summary>
        /// Gets the content item that contains the Rich text element with the content link that is being resolved.
        /// </summary>
        public ContentItem ContentItem
        {
            get; internal set;
        }
    }
}
