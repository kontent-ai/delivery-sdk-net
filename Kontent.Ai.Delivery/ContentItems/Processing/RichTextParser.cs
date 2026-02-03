using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.ContentItems.RichText;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.ContentItems.Processing;

internal class RichTextParser(
    IHtmlParser parser,
    IContentDependencyExtractor dependencyExtractor,
    ILogger? logger = null)
{
    /// <summary>
    /// Maximum depth for recursive rich text parsing to prevent stack overflow on deeply nested content.
    /// </summary>
    private const int MaxParsingDepth = 100; // TODO: confirm depth, compare with 124 from RefitSettingsProvider
    /// <summary>
    /// Converts a rich text element to structured content.
    /// </summary>
    internal async Task<IRichTextContent?> ConvertAsync<TElement>(
        TElement contentElement,
        Func<string, Task<object?>> getLinkedItem,
        DependencyTrackingContext? dependencyContext) where TElement : IContentElementValue<string>
    {
        if (contentElement is not IRichTextElementValue element)
            return null;

        // Parsing the HTML itself is synchronous; keep async only for linked-item resolution.
        var document = parser.ParseDocument(element.Value);

        if (document.Body == null)
        {
            if (logger is not null)
            {
                LoggerMessages.RichTextParsingFailed(logger, element.Codename);
            }
            throw new InvalidOperationException("Failed to parse rich text HTML: document body is null.");
        }

        // Extract dependencies for caching (delegated to extractor)
        dependencyExtractor.ExtractFromRichTextElement(element, dependencyContext);

        var blocks = new List<IRichTextBlock>();
        foreach (var childNode in document.Body.ChildNodes)
        {
            var block = await ParseNodeAsync(childNode, element, getLinkedItem, currentDepth: 0);
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
        Func<string, Task<object?>> getLinkedItem,
        int currentDepth)
    {
        // Guard against excessive recursion depth to prevent stack overflow
        if (currentDepth > MaxParsingDepth)
        {
            if (logger is not null)
            {
                LoggerMessages.RichTextMaxDepthExceeded(logger, MaxParsingDepth);
            }
            return null;
        }

        return node switch
        {
            // Parse special Kontent.ai elements
            IElement { TagName: "OBJECT" } el
                => await ParseEmbeddedContentAsync(el, getLinkedItem),

            IElement { TagName: "FIGURE" } el when TryGetInlineImage(el, element, out var image)
                => image,

            IElement { TagName: "A" } el when TryGetItemId(el, out var itemId)
                => await ParseContentItemLinkAsync(el, itemId, element, getLinkedItem, currentDepth),

            // Parse all HTML elements into structured tree
            IElement el
                => await ParseHtmlElementAsync(el, element, getLinkedItem, currentDepth),

            // Text nodes become TextNode leaf blocks
            IText text when !string.IsNullOrWhiteSpace(text.TextContent)
                => new TextNode(text.TextContent),

            _ => null
        };
    }


    private async Task<IRichTextBlock?> ParseEmbeddedContentAsync(IElement element, Func<string, Task<object?>> getLinkedItem)
    {
        var codename = element.GetAttribute("data-codename");
        if (string.IsNullOrEmpty(codename))
        {
            if (logger is not null)
            {
                LoggerMessages.EmbeddedContentMissingCodename(logger);
            }
            throw new InvalidOperationException(
                "Embedded item/component is missing required 'data-codename' attribute. " +
                $"Element HTML: {element.OuterHtml}");
        }

        var contentItem = await getLinkedItem(codename);

        // ContentItem<T> implements IEmbeddedContent<T>, so we can cast directly
        // If content item couldn't be resolved (depth limit, etc.), return null
        // Null blocks are filtered out by the caller
        if (contentItem is not IEmbeddedContent embeddedContent)
        {
            if (logger is not null)
            {
                LoggerMessages.EmbeddedContentNotFound(logger, codename);
            }
            return null;
        }

        return embeddedContent;
    }

    private async Task<IRichTextBlock> ParseContentItemLinkAsync(
        IElement anchorElement,
        Guid itemId,
        IRichTextElementValue elementValue,
        Func<string, Task<object?>> getLinkedItem,
        int currentDepth)
    {
        // Get metadata from Links dictionary
        var metadata = elementValue.Links?.TryGetValue(itemId, out var link) == true ? link : null;

        // Parse children recursively
        var children = new List<IRichTextBlock>();
        foreach (var childNode in anchorElement.ChildNodes)
        {
            var childBlock = await ParseNodeAsync(childNode, elementValue, getLinkedItem, currentDepth + 1);
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
        Func<string, Task<object?>> getLinkedItem,
        int currentDepth)
    {
        // Parse all children recursively into tree structure
        var children = new List<IRichTextBlock>();
        foreach (var childNode in element.ChildNodes)
        {
            var childBlock = await ParseNodeAsync(childNode, elementValue, getLinkedItem, currentDepth + 1);
            if (childBlock != null)
                children.Add(childBlock);
        }

        // Extract attributes
        var attributes = element.Attributes.ToDictionary(a => a.Name, a => a.Value);

        return new HtmlNode(element.TagName.ToLowerInvariant(), attributes, children);
    }

    private bool TryGetItemId(IElement element, out Guid itemId)
    {
        var dataItemId = element.GetAttribute("data-item-id");
        if (Guid.TryParse(dataItemId, out itemId))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(dataItemId) && logger is not null)
        {
            LoggerMessages.RichTextLinkIdParsingFailed(logger, dataItemId);
        }
        return false;
    }

    private bool TryGetInlineImage(
        IElement figureBlock,
        IRichTextElementValue element,
        out IInlineImage image)
    {
        var img = figureBlock.Children
            .FirstOrDefault(child => child.TagName?.Equals("img", StringComparison.OrdinalIgnoreCase) == true);

        if (img is null)
        {
            image = null!;
            return false;
        }

        var dataAssetId = img.GetAttribute("data-asset-id");
        if (!Guid.TryParse(dataAssetId, out var assetId))
        {
            image = null!;
            return false;
        }

        if (element.Images?.TryGetValue(assetId, out var inlineImage) == true)
        {
            image = inlineImage;
            return true;
        }

        if (logger is not null)
        {
            LoggerMessages.InlineImageNotFound(logger, assetId);
        }
        image = null!;
        return false;
    }
}
