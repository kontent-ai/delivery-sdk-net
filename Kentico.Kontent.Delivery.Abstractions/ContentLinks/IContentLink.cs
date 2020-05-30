namespace Kentico.Kontent.Delivery.Abstractions.ContentLinks
{
    /// <summary>
    /// Represents a link to a content item in a Rich text element.
    /// </summary>
    public interface IContentLink
    {
        /// <summary>
        /// Gets the codename of the linked content item.
        /// </summary>
        string Codename { get; }

        /// <summary>
        /// Gets the content type codename of the linked content item.
        /// </summary>
        string ContentTypeCodename { get; }

        /// <summary>
        /// Gets the identifier of the linked content item.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the URL slug of the linked content item, if available; otherwise, <c>null</c>.
        /// </summary>
        string UrlSlug { get; }
    }
}