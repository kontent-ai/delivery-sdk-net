using Kontent.Ai.Delivery.ContentItems.RichText.Resolution;

namespace Kontent.Ai.Delivery;

/// <summary>
/// Extension methods for working with rich text content.
/// </summary>
public static class RichTextExtensions
{
    /// <summary>
    /// Converts structured rich text content to an HTML string.
    /// </summary>
    /// <param name="richText">The structured rich text content.</param>
    /// <param name="resolver">Optional HTML resolver. If null, uses default resolvers.</param>
    /// <returns>The HTML representation of the rich text content.</returns>
    public static ValueTask<string> ToHtmlAsync(
        this IRichTextContent richText,
        IHtmlResolver? resolver = null)
    {
        ArgumentNullException.ThrowIfNull(richText);

        resolver ??= new HtmlResolverBuilder().Build();

        return resolver.ResolveAsync(richText);
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
}