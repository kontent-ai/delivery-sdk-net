using System.Collections;

namespace Kontent.Ai.Delivery.ContentItems.RichText;

/// <inheritdoc cref="IRichTextContent" />
public sealed class RichTextContent : IRichTextContent
{
    private readonly List<IRichTextBlock> _blocks = [];

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

    /// <summary>
    /// Adds multiple blocks to the rich text content.
    /// </summary>
    /// <param name="blocks">The blocks to add.</param>
    internal void AddRange(IEnumerable<IRichTextBlock> blocks) => _blocks.AddRange(blocks);

    /// <inheritdoc />
    public int Count => _blocks.Count;

    /// <inheritdoc />
    public IRichTextBlock this[int index] => _blocks[index];

    /// <inheritdoc />
    public IEnumerator<IRichTextBlock> GetEnumerator() => _blocks.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
