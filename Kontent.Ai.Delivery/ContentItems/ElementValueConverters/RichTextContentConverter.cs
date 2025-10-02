using System.Reflection;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.ContentItems.RichText;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

namespace Kontent.Ai.Delivery.ContentItems;

internal class RichTextContentConverter(IHtmlParser parser) : IPropertyValueConverter<string, IRichTextContent>
{
    private const int MaxRecursionDepth = 100;

    public async Task<IRichTextContent?> GetPropertyValueAsync<TElement>(
        PropertyInfo property,
        TElement contentElement,
        ResolvingContext context) where TElement : IContentElementValue<string>
    {
        if (!typeof(IRichTextContent).IsAssignableFrom(property.PropertyType))
            throw new InvalidOperationException($"Type of property {property.Name} must implement {nameof(IRichTextContent)} in order to receive rich text content.");

        if (contentElement is not IRichTextElementValue element)
            return null;

        var document = await parser.ParseDocumentAsync(element.Value);

        var blocks = new List<IRichTextBlock>();
        foreach (var childNode in document.Body.ChildNodes)
        {
            var block = await ParseNodeAsync(childNode, element, context, depth: 0);
            if (block != null)
                blocks.Add(block);
        }

        var content = new RichTextContent
        {
            Links = element.Links != null
                ? new Dictionary<Guid, IContentLink>(element.Links)
                : null,
            Images = element.Images != null
                ? new Dictionary<Guid, IInlineImage>(element.Images)
                : null,
            ModularContentCodenames = element.ModularContent
        };
        content.AddRange(blocks);
        return content;
    }

    private async Task<IRichTextBlock?> ParseNodeAsync(
        INode node,
        IRichTextElementValue element,
        ResolvingContext context,
        int depth)
    {
        if (depth > MaxRecursionDepth)
        {
            throw new InvalidOperationException(
                $"Rich text content exceeds maximum nesting depth of {MaxRecursionDepth}. " +
                "This may indicate malformed HTML or excessively nested content.");
        }

        return node switch
        {
            // Parse special Kontent.ai elements
            IElement { TagName: "OBJECT" } el when IsInlineContentItem(el)
                => await ParseInlineContentItemAsync(el, context),

            IElement { TagName: "FIGURE" } el when TryGetInlineImage(el, element, out var image)
                => image,

            IElement { TagName: "A" } el when TryGetItemId(el, out var itemId)
                => await ParseContentItemLinkAsync(el, itemId, element, context, depth),

            // Parse HTML elements with potential structured children (containing links, etc.)
            IElement el when ShouldParseChildren(el)
                => await ParseHtmlElementAsync(el, element, context, depth),

            // Elements without structured children - keep as opaque HTML
            IElement el
                => new HtmlContent { Html = el.OuterHtml },

            // Text nodes
            IText text when !string.IsNullOrWhiteSpace(text.TextContent)
                => new HtmlContent { Html = text.TextContent },

            _ => null
        };
    }


    private async Task<IRichTextBlock> ParseInlineContentItemAsync(IElement element, ResolvingContext context)
    {
        var codename = element.GetAttribute("data-codename");
        if (string.IsNullOrEmpty(codename))
        {
            throw new InvalidOperationException(
                "Inline content item is missing required 'data-codename' attribute. " +
                $"Element HTML: {element.OuterHtml}");
        }

        var contentItem = await context.GetLinkedItem(codename);
        return new InlineContentItem(contentItem);
    }

    private async Task<IRichTextBlock> ParseContentItemLinkAsync(
        IElement anchorElement,
        Guid itemId,
        IRichTextElementValue elementValue,
        ResolvingContext context,
        int depth)
    {
        // Get metadata from Links dictionary
        var metadata = elementValue.Links?.TryGetValue(itemId, out var link) == true ? link : null;

        // Parse children recursively with incremented depth
        var children = new List<IRichTextBlock>();
        foreach (var childNode in anchorElement.ChildNodes)
        {
            var childBlock = await ParseNodeAsync(childNode, elementValue, context, depth + 1);
            if (childBlock != null)
                children.Add(childBlock);
        }

        // Extract other attributes (excluding data-item-id)
        var attributes = anchorElement.Attributes
            .Where(a => a.Name != "data-item-id")
            .ToDictionary(a => a.Name, a => a.Value);

        return new ContentItemLink(itemId, metadata, children, attributes);
    }

    private async Task<IRichTextBlock> ParseHtmlElementAsync(
        IElement element,
        IRichTextElementValue elementValue,
        ResolvingContext context,
        int depth)
    {
        // Check if this element or any descendants contain special elements (links, images, etc.)
        var hasStructuredContent = await HasStructuredContentAsync(element, elementValue);

        if (!hasStructuredContent)
        {
            // No structured content - return as opaque HTML
            return new HtmlContent { Html = element.OuterHtml };
        }

        // Parse children recursively
        var children = new List<IRichTextBlock>();
        foreach (var childNode in element.ChildNodes)
        {
            var childBlock = await ParseNodeAsync(childNode, elementValue, context, depth + 1);
            if (childBlock != null)
                children.Add(childBlock);
        }

        // Extract attributes
        var attributes = element.Attributes.ToDictionary(a => a.Name, a => a.Value);

        return new HtmlElement(element.TagName.ToLowerInvariant(), attributes, children);
    }

    private async Task<bool> HasStructuredContentAsync(IElement element, IRichTextElementValue elementValue)
    {
        // Check current element
        if (element.TagName == "A" && TryGetItemId(element, out _))
            return true;
        if (element.TagName == "OBJECT" && IsInlineContentItem(element))
            return true;
        if (element.TagName == "FIGURE" && TryGetInlineImage(element, elementValue, out _))
            return true;

        // Check descendants recursively
        foreach (var child in element.Children)
        {
            if (await HasStructuredContentAsync(child, elementValue))
                return true;
        }

        return false;
    }

    private static bool ShouldParseChildren(IElement element)
    {
        // Parse common HTML elements that can contain structured content
        var parseableTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "P", "DIV", "H1", "H2", "H3", "H4", "H5", "H6",
            "UL", "OL", "LI",
            "BLOCKQUOTE", "PRE",
            "TABLE", "THEAD", "TBODY", "TFOOT", "TR", "TD", "TH",
            "STRONG", "EM", "B", "I", "U", "SPAN", "CODE"
        };

        return parseableTags.Contains(element.TagName);
    }

    private static bool IsInlineContentItem(IElement block) =>
        block.GetAttribute("type") == "application/kenticocloud" &&
        block.GetAttribute("data-type") == "item";

    private static bool TryGetItemId(IElement element, out Guid itemId)
    {
        var dataItemId = element.GetAttribute("data-item-id");
        return Guid.TryParse(dataItemId, out itemId);
    }

    private static bool TryGetInlineImage(
        IElement figureBlock,
        IRichTextElementValue element,
        out IInlineImage image)
    {
        var img = figureBlock.Children
            .FirstOrDefault(child => child.TagName?.Equals("img", StringComparison.OrdinalIgnoreCase) == true);

        if (img is not null &&
            Guid.TryParse(img.GetAttribute("data-asset-id"), out var assetId) &&
            element.Images.TryGetValue(assetId, out var inlineImage))
        {
            image = inlineImage;
            return true;
        }

        image = null!;
        return false;
    }
}
