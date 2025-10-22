using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

/// <inheritdoc cref="IContentItemLink" />
[DebuggerDisplay("ItemId = {ItemId}, Codename = {Metadata?.Codename}")]
internal record ContentItemLink(
    Guid ItemId,
    IContentLink? Metadata,
    IReadOnlyList<IRichTextBlock> Children,
    IReadOnlyDictionary<string, string> Attributes
) : IContentItemLink
{
    /// <summary>
    /// Default constructor for JSON deserialization.
    /// </summary>
    [JsonConstructor]
    public ContentItemLink() : this(Guid.Empty, null, Array.Empty<IRichTextBlock>(), new Dictionary<string, string>())
    {
    }
}