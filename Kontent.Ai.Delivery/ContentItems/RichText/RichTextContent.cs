namespace Kontent.Ai.Delivery.ContentItems.RichText;

/// <inheritdoc cref="IRichTextContent" />
public class RichTextContent() : List<IRichTextBlock>, IRichTextContent
{
    /// <summary>
    /// Link metadata for resolving content item links.
    /// Internal property used during HTML resolution.
    /// </summary>
    internal IReadOnlyDictionary<Guid, IContentLink>? Links { get; set; }

    /// <summary>
    /// Image metadata for resolving inline images.
    /// Internal property used during HTML resolution.
    /// </summary>
    internal IReadOnlyDictionary<Guid, IInlineImage>? Images { get; set; }

    /// <summary>
    /// Modular content codenames for resolving inline content items.
    /// Internal property used during HTML resolution.
    /// </summary>
    internal IReadOnlyList<string>? ModularContentCodenames { get; set; }
}
