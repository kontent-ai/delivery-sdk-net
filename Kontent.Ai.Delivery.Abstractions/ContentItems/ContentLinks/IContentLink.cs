namespace Kontent.Ai.Delivery.Abstractions;

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
    Guid Id { get; }

    /// <summary>
    /// Gets the URL slug of the linked content item.
    /// Empty string if the linked content item has no URL slug.
    /// </summary>
    string UrlSlug { get; }
}
