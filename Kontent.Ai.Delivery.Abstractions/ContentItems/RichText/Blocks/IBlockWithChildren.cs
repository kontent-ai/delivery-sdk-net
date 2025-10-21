using System;
using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a block with nested children.
/// </summary>
public interface IBlockWithChildren : IRichTextBlock
{
    /// <summary>
    /// The nested rich text blocks contained within a tag.
    /// </summary>
    IReadOnlyList<IRichTextBlock> Children { get; }
}
