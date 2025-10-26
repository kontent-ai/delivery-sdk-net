using System.Collections.ObjectModel;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

/// <summary>
/// Represents an HTML element block with structured children
/// </summary>
internal sealed record HtmlNode(
    string TagName,
    IReadOnlyDictionary<string, string> Attributes,
    IReadOnlyList<IRichTextBlock> Children
) : IHtmlNode;