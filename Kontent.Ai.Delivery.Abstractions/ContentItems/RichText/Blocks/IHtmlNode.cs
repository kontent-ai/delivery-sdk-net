namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents an HTML element block with structured children
/// </summary>
public interface IHtmlNode : IBlockWithChildren
{
    /// <summary>
    /// HTML tag name (e.g., "p", "li", "h1")
    /// </summary>
    string TagName { get; }

    /// <summary>
    /// HTML attributes (e.g., class, id)
    /// </summary>
    IReadOnlyDictionary<string, string> Attributes { get; }
}
