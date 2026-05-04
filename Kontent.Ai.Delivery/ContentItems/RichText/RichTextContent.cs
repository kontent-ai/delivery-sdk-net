using System.Collections;
using System.Collections.Frozen;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

namespace Kontent.Ai.Delivery.ContentItems.RichText;

/// <inheritdoc cref="IRichTextContent" />
public sealed class RichTextContent : IRichTextContent
{
    private readonly IReadOnlyList<IRichTextBlock> _blocks;

    internal RichTextContent(
        IReadOnlyList<IRichTextBlock> blocks,
        IReadOnlyDictionary<Guid, IContentLink>? links = null,
        IReadOnlyDictionary<Guid, IInlineImage>? images = null,
        IReadOnlyList<string>? modularContentCodenames = null)
    {
        _blocks = blocks;
        Links = links;
        Images = images;
        ModularContentCodenames = modularContentCodenames;
    }

    /// <summary>
    /// The default Kontent.ai empty rich text value, equivalent to <c>&lt;p&gt;&lt;br&gt;&lt;/p&gt;</c>.
    /// </summary>
    public static RichTextContent Empty { get; } = new(
    [
        new HtmlNode(
            "p",
            FrozenDictionary<string, string>.Empty,
            [
                new HtmlNode(
                    "br",
                    FrozenDictionary<string, string>.Empty,
                    [])
            ])
    ]);

    /// <summary>
    /// Link metadata for resolving content item links.
    /// Internal property used during HTML resolution.
    /// </summary>
    internal IReadOnlyDictionary<Guid, IContentLink>? Links { get; }

    /// <summary>
    /// Image metadata for resolving inline images.
    /// Internal property used during HTML resolution.
    /// </summary>
    internal IReadOnlyDictionary<Guid, IInlineImage>? Images { get; }

    /// <summary>
    /// Modular content codenames for resolving inline content items.
    /// Internal property used during HTML resolution.
    /// </summary>
    internal IReadOnlyList<string>? ModularContentCodenames { get; }

    /// <inheritdoc />
    public int Count => _blocks.Count;

    /// <inheritdoc />
    public IRichTextBlock this[int index] => _blocks[index];

    /// <inheritdoc />
    public IEnumerator<IRichTextBlock> GetEnumerator() => _blocks.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
