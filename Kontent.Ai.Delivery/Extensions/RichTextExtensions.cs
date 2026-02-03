using System.Text.Json;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Elements;
using Kontent.Ai.Delivery.ContentItems.Processing;
using Kontent.Ai.Delivery.ContentItems.RichText.Resolution;
using Kontent.Ai.Delivery.Configuration;

namespace Kontent.Ai.Delivery;

/// <summary>
/// Extension methods for working with rich text content.
/// </summary>
public static class RichTextExtensions
{
    private static readonly JsonSerializerOptions RichTextElementDeserializerOptions = new()
    {
        Converters = { new RichTextElementDataConverter() }
    };

    // Cached parser and options for ParseRichTextAsync - safe to reuse as HtmlParser.ParseDocument returns new documents
    private static readonly Lazy<RichTextParser> DefaultRichTextParser = new(
        () => RichTextParser.CreateDefault(),
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<JsonSerializerOptions> ContentItemDeserializerOptions = new(
        () => RefitSettingsProvider.CreateDefaultJsonSerializerOptions(),
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Converts structured rich text content to an HTML string.
    /// </summary>
    /// <param name="richText">The structured rich text content.</param>
    /// <param name="resolver">Optional HTML resolver. If null, uses default resolvers.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The HTML representation of the rich text content.</returns>
    public static ValueTask<string> ToHtmlAsync(
        this IRichTextContent richText,
        IHtmlResolver? resolver = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(richText);

        resolver ??= new HtmlResolverBuilder().Build();

        return resolver.ResolveAsync(richText, cancellationToken);
    }

    /// <summary>
    /// Retrieves all blocks of a specific type from rich text content, including nested blocks.
    /// Uses recursive traversal to find blocks within content item links.
    /// </summary>
    /// <typeparam name="TBlock">The type of block to retrieve.</typeparam>
    /// <param name="richText">The rich text content to search.</param>
    /// <returns>An enumerable of all blocks of the specified type.</returns>
    public static IEnumerable<TBlock> GetBlocks<TBlock>(this IRichTextContent richText)
        where TBlock : IRichTextBlock
    {
        ArgumentNullException.ThrowIfNull(richText);

        return richText.SelectMany(GetBlocksRecursive<TBlock>);
    }

    /// <summary>
    /// Recursively traverses a block and its children to find all blocks of a specific type.
    /// </summary>
    private static IEnumerable<TBlock> GetBlocksRecursive<TBlock>(IRichTextBlock block)
        where TBlock : IRichTextBlock
    {
        if (block is TBlock typedBlock)
            yield return typedBlock;

        if (block is IBlockWithChildren blockWithChildren)
        {
            foreach (var child in blockWithChildren.Children.SelectMany(GetBlocksRecursive<TBlock>))
                yield return child;
        }
    }

    /// <summary>
    /// Gets all content item links from rich text content, including nested links.
    /// </summary>
    /// <param name="richText">The rich text content to search.</param>
    /// <returns>An enumerable of all content item links.</returns>
    public static IEnumerable<IContentItemLink> GetContentItemLinks(this IRichTextContent richText)
    {
        return richText.GetBlocks<IContentItemLink>();
    }

    /// <summary>
    /// Gets all embedded content (components and linked items) from the rich text content.
    /// </summary>
    /// <param name="richText">The rich text content to search.</param>
    /// <returns>An enumerable of all embedded content blocks.</returns>
    public static IEnumerable<IEmbeddedContent> GetEmbeddedContent(this IRichTextContent richText)
    {
        return richText.GetBlocks<IEmbeddedContent>();
    }

    /// <summary>
    /// Filters rich text blocks to return only strongly-typed embedded content of a specific model type.
    /// </summary>
    /// <typeparam name="TModel">The model type to filter by.</typeparam>
    /// <param name="richText">The rich text content to filter.</param>
    /// <returns>A sequence of strongly-typed embedded content matching the specified model type.</returns>
    /// <example>
    /// <code>
    /// // Get all embedded articles from rich text
    /// var articles = richText.GetEmbeddedContent&lt;Article&gt;();
    /// foreach (var article in articles)
    /// {
    ///     Console.WriteLine(article.Elements.Title);
    /// }
    /// </code>
    /// </example>
    public static IEnumerable<IEmbeddedContent<TModel>> GetEmbeddedContent<TModel>(
        this IRichTextContent richText)
    {
        ArgumentNullException.ThrowIfNull(richText);

        return richText.OfType<IEmbeddedContent<TModel>>();
    }

    /// <summary>
    /// Filters a sequence of rich text blocks to return only strongly-typed embedded content of a specific model type.
    /// </summary>
    /// <typeparam name="TModel">The model type to filter by.</typeparam>
    /// <param name="blocks">The sequence of rich text blocks to filter.</param>
    /// <returns>A sequence of strongly-typed embedded content matching the specified model type.</returns>
    /// <example>
    /// <code>
    /// // Get all embedded coffee products from rich text blocks
    /// var coffees = richText
    ///     .GetEmbeddedContentOfType&lt;Coffee&gt;()
    ///     .Select(c => c.Elements);
    /// </code>
    /// </example>
    public static IEnumerable<IEmbeddedContent<TModel>> GetEmbeddedContentOfType<TModel>(
        this IEnumerable<IRichTextBlock> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        return blocks.OfType<IEmbeddedContent<TModel>>();
    }

    /// <summary>
    /// Extracts the strongly-typed elements from all embedded content of a specific model type in rich text.
    /// </summary>
    /// <typeparam name="TModel">The model type to extract.</typeparam>
    /// <param name="richText">The rich text content to process.</param>
    /// <returns>A sequence of strongly-typed element models.</returns>
    /// <example>
    /// <code>
    /// // Get all article element models directly
    /// var articleElements = richText.GetEmbeddedElements&lt;Article&gt;();
    /// foreach (var article in articleElements)
    /// {
    ///     Console.WriteLine(article.Title);
    /// }
    /// </code>
    /// </example>
    public static IEnumerable<TModel> GetEmbeddedElements<TModel>(
        this IRichTextContent richText)
    {
        ArgumentNullException.ThrowIfNull(richText);

        return richText
            .OfType<IEmbeddedContent<TModel>>()
            .Select(e => e.Elements);
    }

    /// <summary>
    /// Gets all inline images from rich text content.
    /// </summary>
    /// <param name="richText">The rich text content to search.</param>
    /// <returns>An enumerable of all inline images.</returns>
    public static IEnumerable<IInlineImage> GetInlineImages(this IRichTextContent richText)
    {
        return richText.GetBlocks<IInlineImage>();
    }

    /// <summary>
    /// Parses a raw rich text element from JSON into structured <see cref="IRichTextContent"/>.
    /// For use with dynamic mode where elements are stored as <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="richTextElement">The raw rich text element JSON from a dynamic content item's elements dictionary.</param>
    /// <param name="modularContent">Optional modular_content dictionary for embedded item resolution.
    /// Pass the ModularContent from the delivery response to enable embedded content resolution.</param>
    /// <param name="cancellationToken">A cancellation token to observe while parsing.</param>
    /// <returns>The parsed rich text content, or null if parsing fails or the element is not a rich text type.</returns>
    /// <example>
    /// <code>
    /// // Fetch dynamic item
    /// var result = await client.Items
    ///     .GetDynamic("article-codename")
    ///     .ExecuteAsync();
    ///
    /// var item = result.Value;
    /// var elements = item.Elements; // IDynamicElements (Dictionary&lt;string, JsonElement&gt;)
    ///
    /// // Parse rich text element
    /// if (elements.TryGetValue("body_copy", out var bodyElement))
    /// {
    ///     var richText = await bodyElement.ParseRichTextAsync(result.ModularContent);
    ///
    ///     if (richText != null)
    ///     {
    ///         // Now RichTextExtensions work!
    ///         var html = await richText.ToHtmlAsync();
    ///         var images = richText.GetInlineImages();
    ///         var embedded = richText.GetEmbeddedContent();
    ///     }
    /// }
    /// </code>
    /// </example>
    public static async ValueTask<IRichTextContent?> ParseRichTextAsync(
        this JsonElement richTextElement,
        IReadOnlyDictionary<string, JsonElement>? modularContent = null,
        CancellationToken cancellationToken = default)
    {
        // Validate that we have a valid JSON object
        if (richTextElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        // Optionally validate it's actually a rich_text type
        if (richTextElement.TryGetProperty("type", out var typeEl) &&
            !string.Equals(typeEl.GetString(), "rich_text", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        RichTextElementData? elementData;
        try
        {
            elementData = JsonSerializer.Deserialize<RichTextElementData>(richTextElement, RichTextElementDeserializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (elementData is null)
        {
            return null;
        }

        // Build linked item resolver that deserializes from modularContent
        var getLinkedItem = CreateLinkedItemResolver(modularContent);

        // Parse the rich text using cached parser
        return await DefaultRichTextParser.Value.ConvertAsync(elementData, getLinkedItem, dependencyContext: null, cancellationToken);
    }

    /// <summary>
    /// Creates a linked item resolver function for dynamic mode parsing.
    /// Resolves embedded content from the modular_content dictionary as ContentItem&lt;IDynamicElements&gt;.
    /// </summary>
    private static Func<string, Task<object?>> CreateLinkedItemResolver(
        IReadOnlyDictionary<string, JsonElement>? modularContent)
    {
        if (modularContent is null || modularContent.Count == 0)
        {
            return _ => Task.FromResult<object?>(null);
        }

        // Cache for resolved items to avoid re-deserializing the same item
        var resolvedItems = new Dictionary<string, IContentItem>(StringComparer.Ordinal);

        // Use cached JSON options with ContentItemConverterFactory for proper deserialization
        var jsonOptions = ContentItemDeserializerOptions.Value;

        return codename =>
        {
            // Check cache first
            if (resolvedItems.TryGetValue(codename, out var cached))
            {
                return Task.FromResult<object?>(cached);
            }

            // Look up in modular content
            if (!modularContent.TryGetValue(codename, out var itemJson))
            {
                return Task.FromResult<object?>(null);
            }

            try
            {
                // Deserialize to ContentItem<IDynamicElements>
                // ContentItem<T> implements IEmbeddedContent<T>, so this works directly
                var contentItem = JsonSerializer.Deserialize<ContentItem<IDynamicElements>>(itemJson, jsonOptions);

                if (contentItem is not null)
                {
                    resolvedItems[codename] = contentItem;
                }

                return Task.FromResult<object?>(contentItem);
            }
            catch (JsonException)
            {
                return Task.FromResult<object?>(null);
            }
        };
    }
}