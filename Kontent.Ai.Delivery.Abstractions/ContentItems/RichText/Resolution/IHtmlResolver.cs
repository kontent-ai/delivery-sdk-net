using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions.ContentItems.RichText.Blocks;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Functional resolver delegate for transforming rich text blocks into HTML strings.
/// </summary>
/// <typeparam name="TBlock">The type of rich text block to resolve.</typeparam>
/// <param name="block">The block to resolve.</param>
/// <param name="context">The resolution context containing shared state.</param>
/// <param name="resolveChildren">Function to resolve child blocks recursively.</param>
/// <returns>The HTML representation of the block.</returns>
public delegate ValueTask<string> BlockResolver<in TBlock>(
    TBlock block,
    IHtmlResolutionContext context,
    Func<IEnumerable<IRichTextBlock>, ValueTask<string>> resolveChildren
) where TBlock : IRichTextBlock;

/// <summary>
/// Predicate delegate for determining if an HTML node should be handled by a specific resolver.
/// </summary>
/// <param name="node">The HTML node to evaluate.</param>
/// <returns>True if the resolver should handle this node; otherwise, false.</returns>
public delegate bool HtmlNodePredicate(IHtmlNode node);

/// <summary>
/// Resolves structured rich text content into HTML strings.
/// </summary>
public interface IHtmlResolver
{
    /// <summary>
    /// Resolves rich text content into an HTML string.
    /// </summary>
    /// <param name="richText">The structured rich text content to resolve.</param>
    /// <param name="context">Optional resolution context for passing shared state.</param>
    /// <returns>The HTML representation of the rich text content.</returns>
    ValueTask<string> ResolveAsync(IRichTextContent richText, IHtmlResolutionContext? context = null);
}

/// <summary>
/// Context for HTML resolution, providing access to linked items and other shared state.
/// </summary>
public interface IHtmlResolutionContext
{
    /// <summary>
    /// Dictionary of linked content items by codename, used for resolving inline content items.
    /// </summary>
    IReadOnlyDictionary<string, object>? LinkedItems { get; }

    /// <summary>
    /// Cancellation token for async operations.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Optional service provider for dependency injection scenarios.
    /// </summary>
    IServiceProvider? Services { get; }
}

/// <summary>
/// Builder interface for fluent configuration of HTML resolvers.
/// </summary>
public interface IHtmlResolverBuilder
{
    /// <summary>
    /// Registers a resolver for content item link blocks.
    /// </summary>
    /// <param name="resolver">The resolver function.</param>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithContentItemLinkResolver(BlockResolver<IContentItemLink> resolver);

    /// <summary>
    /// Registers a resolver for inline content item blocks.
    /// </summary>
    /// <param name="resolver">The resolver function.</param>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithInlineContentItemResolver(BlockResolver<IInlineContentItem> resolver);

    /// <summary>
    /// Registers a resolver for inline image blocks.
    /// </summary>
    /// <param name="resolver">The resolver function.</param>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithInlineImageResolver(BlockResolver<IInlineImage> resolver);

    /// <summary>
    /// Registers a resolver for text node blocks (leaf text content).
    /// </summary>
    /// <param name="resolver">The resolver function.</param>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithTextNodeResolver(BlockResolver<ITextNode> resolver);

    /// <summary>
    /// Registers a conditional resolver for HTML nodes matching a predicate.
    /// Resolvers are evaluated in registration order - first match wins.
    /// If no conditional resolver matches, falls back to <see cref="WithHtmlElementResolver"/>.
    /// </summary>
    /// <param name="predicate">Predicate to determine if this resolver applies to a node.</param>
    /// <param name="resolver">The resolver function for matching nodes.</param>
    /// <param name="description">Optional description for debugging purposes.</param>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithHtmlNodeResolver(
        HtmlNodePredicate predicate,
        BlockResolver<IHtmlNode> resolver,
        string? description = null);

    /// <summary>
    /// Convenience method to register a resolver for HTML nodes with a specific tag name.
    /// Tag name matching is case-insensitive.
    /// </summary>
    /// <param name="tagName">The HTML tag name to match (e.g., "h1", "p", "div").</param>
    /// <param name="resolver">The resolver function for matching nodes.</param>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithHtmlNodeResolver(
        string tagName,
        BlockResolver<IHtmlNode> resolver);

    /// <summary>
    /// Convenience method to register a resolver for HTML nodes with a specific attribute.
    /// </summary>
    /// <param name="attributeName">The attribute name to check for.</param>
    /// <param name="attributeValue">Optional specific attribute value to match (null matches any value).</param>
    /// <param name="resolver">The resolver function for matching nodes.</param>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithHtmlNodeResolverForAttribute(
        string attributeName,
        string? attributeValue,
        BlockResolver<IHtmlNode> resolver);

    /// <summary>
    /// Registers a fallback resolver for HTML element blocks with structured children.
    /// This resolver is used when no conditional resolver matches via <see cref="WithHtmlNodeResolver"/>.
    /// </summary>
    /// <param name="resolver">The resolver function.</param>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithHtmlElementResolver(BlockResolver<IHtmlNode> resolver);

    /// <summary>
    /// Builds the configured HTML resolver.
    /// </summary>
    /// <returns>A new HTML resolver instance.</returns>
    IHtmlResolver Build();
}
