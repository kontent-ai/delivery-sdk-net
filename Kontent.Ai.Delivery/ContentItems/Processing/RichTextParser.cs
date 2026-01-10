using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.ContentItems.RichText;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

namespace Kontent.Ai.Delivery.ContentItems.Processing;

internal class RichTextParser(IHtmlParser parser, IContentDependencyExtractor dependencyExtractor) : IElementValueConverter<string, IRichTextContent>
{
    public async Task<IRichTextContent?> ConvertAsync<TElement>(
        TElement contentElement,
        ResolvingContext context) where TElement : IContentElementValue<string>
    {
        // Public interface method - delegates to internal implementation
        return await ConvertAsync(contentElement, context, null).ConfigureAwait(false);
    }

    /// <summary>
    /// Internal conversion method with dependency tracking support.
    /// </summary>
    internal async Task<IRichTextContent?> ConvertAsync<TElement>(
        TElement contentElement,
        ResolvingContext context,
        DependencyTrackingContext? dependencyContext) where TElement : IContentElementValue<string>
    {
        if (contentElement is not IRichTextElementValue element)
            return null;

        // Parsing the HTML itself is synchronous; keep async only for linked-item resolution.
        var document = parser.ParseDocument(element.Value);

        if (document.Body == null)
            throw new InvalidOperationException("Failed to parse rich text HTML: document body is null.");

        // Extract dependencies for caching (delegated to extractor)
        dependencyExtractor.ExtractFromRichTextElement(element, dependencyContext);

        var blocks = new List<IRichTextBlock>();
        foreach (var childNode in document.Body.ChildNodes)
        {
            var block = await ParseNodeAsync(childNode, element, context);
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
        ResolvingContext context)
    {
        return node switch
        {
            // Parse special Kontent.ai elements
            IElement { TagName: "OBJECT" } el
                => await ParseInlineContentItemAsync(el, context),

            IElement { TagName: "FIGURE" } el when TryGetInlineImage(el, element, out var image)
                => image,

            IElement { TagName: "A" } el when TryGetItemId(el, out var itemId)
                => await ParseContentItemLinkAsync(el, itemId, element, context),

            // Parse all HTML elements into structured tree
            IElement el
                => await ParseHtmlElementAsync(el, element, context),

            // Text nodes become TextNode leaf blocks
            IText text when !string.IsNullOrWhiteSpace(text.TextContent)
                => new TextNode(text.TextContent),

            _ => null
        };
    }


    private async Task<IRichTextBlock?> ParseInlineContentItemAsync(IElement element, ResolvingContext context)
    {
        var codename = element.GetAttribute("data-codename");
        if (string.IsNullOrEmpty(codename))
        {
            throw new InvalidOperationException(
                "Inline content item is missing required 'data-codename' attribute. " +
                $"Element HTML: {element.OuterHtml}");
        }

        var contentItem = await context.GetLinkedItem(codename);

        // ContentItem<T> implements IEmbeddedContent<T>, so we can cast directly
        // If content item couldn't be resolved (depth limit, etc.), return null
        // Null blocks are filtered out by the caller
        return contentItem as IEmbeddedContent;
    }

    private async Task<IRichTextBlock> ParseContentItemLinkAsync(
        IElement anchorElement,
        Guid itemId,
        IRichTextElementValue elementValue,
        ResolvingContext context)
    {
        // Get metadata from Links dictionary
        var metadata = elementValue.Links?.TryGetValue(itemId, out var link) == true ? link : null;

        // Parse children recursively
        var children = new List<IRichTextBlock>();
        foreach (var childNode in anchorElement.ChildNodes)
        {
            var childBlock = await ParseNodeAsync(childNode, elementValue, context);
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
        ResolvingContext context)
    {
        // Parse all children recursively into tree structure
        var children = new List<IRichTextBlock>();
        foreach (var childNode in element.ChildNodes)
        {
            var childBlock = await ParseNodeAsync(childNode, elementValue, context);
            if (childBlock != null)
                children.Add(childBlock);
        }

        // Extract attributes
        var attributes = element.Attributes.ToDictionary(a => a.Name, a => a.Value);

        return new HtmlNode(element.TagName.ToLowerInvariant(), attributes, children);
    }

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
