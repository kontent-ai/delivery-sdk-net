namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Defines the contract to resolve content links in Rich text element values.
    /// </summary>
    public interface IContentLinkUrlResolver
    {
        /// <summary>
        /// Returns a URL of the linked content item.
        /// </summary>
        /// <param name="link">The link to a content item that needs to be resolved.</param>
        /// <param name="context">The context in which the content link is resolved.</param>
        /// <returns>The URL of the linked content item, if possible; otherwise, <c>null</c>.</returns>
        string ResolveLinkUrl(ContentLink link, ContentLinkUrlResolverContext context);

        /// <summary>
        /// Returns a URL of the linked content item that is not available.
        /// </summary>
        /// <param name="context">The context in which the content link is resolved.</param>
        /// <returns>The URL of the linked content item that is not available, if possible; otherwise, <c>null</c>.</returns>
        string ResolveBrokenLinkUrl(ContentLinkUrlResolverContext context);
    }
}
