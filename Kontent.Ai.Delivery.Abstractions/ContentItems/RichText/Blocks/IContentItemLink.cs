using System;
using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a content item link block within rich text.
/// Content item links are anchor tags with data-item-id attributes that reference other content items.
/// </summary>
public interface IContentItemLink : IRichTextBlock
{
    /// <summary>
    /// The unique identifier of the linked content item.
    /// </summary>
    Guid ItemId { get; }

    /// <summary>
    /// Metadata about the linked content item from the Links collection, if available.
    /// This will be null if the link is broken or the content item was not included in the response.
    /// </summary>
    IContentLink? Metadata { get; }

    /// <summary>
    /// The nested rich text blocks contained within the anchor tag.
    /// These represent the inner content of the link (text, HTML, or even nested elements).
    /// </summary>
    IReadOnlyList<IRichTextBlock> Children { get; }

    /// <summary>
    /// Additional HTML attributes from the anchor tag (excluding data-item-id).
    /// Common attributes include class, target, rel, etc.
    /// </summary>
    IReadOnlyDictionary<string, string> Attributes { get; }
}
