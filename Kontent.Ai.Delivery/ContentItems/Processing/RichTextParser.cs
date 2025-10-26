using System.Collections.Concurrent;
using System.Reflection;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Abstractions.ContentItems.Processing;
using Kontent.Ai.Delivery.ContentItems.RichText;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

namespace Kontent.Ai.Delivery.ContentItems.Processing;

internal class RichTextParser(IHtmlParser parser, IContentDependencyExtractor dependencyExtractor) : IElementValueConverter<string, IRichTextContent>
{
    // Reflection cache for efficient generic type construction
    private static readonly ConcurrentDictionary<Type, ConstructorInfo> _constructorCache = new();
    private static readonly ConcurrentDictionary<Type, Type> _embeddedContentTypeCache = new();
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

        var document = await parser.ParseDocumentAsync(element.Value);

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

        // Handle null (depth limit reached) - return non-generic placeholder
        if (contentItem is null)
        {
            return new EmbeddedContent("unknown", codename, null, Guid.Empty, null);
        }

        // Try to extract type information and create generic EmbeddedContent<T>
        var contentItemType = contentItem.GetType();

        // Check if it's ContentItem<T>
        if (contentItemType.IsGenericType &&
            contentItemType.GetGenericTypeDefinition() == typeof(ContentItem<>))
        {
            var modelType = contentItemType.GetGenericArguments()[0];

            // Get or create cached generic EmbeddedContent<T> type
            var embeddedContentType = _embeddedContentTypeCache.GetOrAdd(
                modelType,
                static t => typeof(EmbeddedContent<>).MakeGenericType(t));

            // Get or create cached constructor
            var constructor = _constructorCache.GetOrAdd(
                modelType,
                static t =>
                {
                    var embeddedType = typeof(EmbeddedContent<>).MakeGenericType(t);
                    return embeddedType.GetConstructor(
                        [typeof(string), typeof(string), typeof(string), typeof(Guid), t])
                        ?? throw new InvalidOperationException($"Constructor not found for EmbeddedContent<{t.Name}>");
                });

            // Extract metadata using dynamic to avoid complex reflection
            dynamic dynamicItem = contentItem;
            var id = Guid.TryParse((string)dynamicItem.System.Id, out var parsedId) ? parsedId : Guid.Empty;

            // Invoke constructor with cached ConstructorInfo
            var embeddedContent = constructor.Invoke(
            [
                (string)dynamicItem.System.Type,
                (string)dynamicItem.System.Codename,
                (string?)dynamicItem.System.Name,
                id,
                dynamicItem.Elements
            ]);

            return (IRichTextBlock)embeddedContent;
        }

        // Fallback to non-generic for unknown types
        if (contentItem is IContentItem<IElementsModel> typedItem)
        {
            var id = Guid.TryParse(typedItem.System.Id, out var parsedId) ? parsedId : Guid.Empty;
            return new EmbeddedContent(
                typedItem.System.Type,
                typedItem.System.Codename,
                typedItem.System.Name,
                id,
                typedItem.Elements);
        }

        // Ultimate fallback
        // TODO: check what happen if this path is hit
        return new EmbeddedContent("unknown", codename, null, Guid.Empty, null);
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
