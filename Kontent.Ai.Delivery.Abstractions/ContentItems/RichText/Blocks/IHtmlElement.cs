using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents an HTML element block with structured children
/// </summary>
public interface IHtmlElement : IRichTextBlock
{
    /// <summary>
    /// HTML tag name (e.g., "p", "li", "h1")
    /// </summary>
    string TagName { get; }

    /// <summary>
    /// HTML attributes (e.g., class, id)
    /// </summary>
    IReadOnlyDictionary<string, string> Attributes { get; }

    /// <summary>
    /// Structured child blocks (can contain text, links, decorators, etc.)
    /// </summary>
    IReadOnlyList<IRichTextBlock> Children { get; }
}
