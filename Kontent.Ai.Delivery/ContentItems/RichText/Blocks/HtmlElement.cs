using System.Collections.ObjectModel;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

/// <summary>
/// Represents an HTML element block with structured children
/// </summary>
internal sealed class HtmlElement : IHtmlElement
{
    public string TagName { get; }

    public IReadOnlyDictionary<string, string> Attributes { get; }

    public IReadOnlyList<IRichTextBlock> Children { get; }

    public HtmlElement(string tagName, IReadOnlyDictionary<string, string>? attributes = null, IReadOnlyList<IRichTextBlock>? children = null)
    {
        TagName = tagName ?? throw new ArgumentNullException(nameof(tagName));
        Attributes = attributes ?? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        Children = children ?? Array.Empty<IRichTextBlock>();
    }
}
