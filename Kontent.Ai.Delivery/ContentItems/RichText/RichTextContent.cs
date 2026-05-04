using System.Collections;
using System.Collections.Frozen;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

namespace Kontent.Ai.Delivery.ContentItems.RichText;

/// <inheritdoc cref="IRichTextContent" />
public sealed class RichTextContent : IRichTextContent
{
    private readonly List<IRichTextBlock> _blocks = [];

    /// <summary>
    /// The default Kontent.ai empty rich text value, equivalent to <c>&lt;p&gt;&lt;br&gt;&lt;/p&gt;</c>.
    /// </summary>
    public static RichTextContent Empty { get; } = CreateEmpty();

    private static RichTextContent CreateEmpty()
    {
        var content = new RichTextContent();
        content.AddRange(
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

        return content;
    }

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
