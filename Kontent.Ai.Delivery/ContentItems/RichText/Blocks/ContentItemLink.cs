using System.Diagnostics;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

/// <inheritdoc cref="IContentItemLink" />
[DebuggerDisplay("ItemId = {ItemId}, Codename = {Metadata?.Codename}")]
internal record ContentItemLink(
    Guid ItemId,
    IContentLink? Metadata,
    IReadOnlyList<IRichTextBlock> Children,
    IReadOnlyDictionary<string, string> Attributes
) : IContentItemLink;
