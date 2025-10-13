using System;
using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a content item link block within rich text.
/// Content item links are anchor tags with data-item-id attributes that reference other content items.
/// </summary>
public interface IBlockWithChildren : IRichTextBlock
{
    /// <summary>
    /// The nested rich text blocks contained within a tag.
    /// </summary>
    IReadOnlyList<IRichTextBlock> Children { get; }
}
