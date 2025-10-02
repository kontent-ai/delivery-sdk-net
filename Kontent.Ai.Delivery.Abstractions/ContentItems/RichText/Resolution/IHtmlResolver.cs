using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
    /// Registers a resolver for HTML content blocks.
    /// </summary>
    /// <param name="resolver">The resolver function.</param>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithHtmlContentResolver(BlockResolver<IHtmlContent> resolver);

    /// <summary>
    /// Registers a resolver for HTML element blocks with structured children.
    /// </summary>
    /// <param name="resolver">The resolver function.</param>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithHtmlElementResolver(BlockResolver<IHtmlElement> resolver);

    /// <summary>
    /// Registers default resolvers for all block types.
    /// Default resolvers provide sensible HTML output for common scenarios.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithDefaultResolvers();

    /// <summary>
    /// Builds the configured HTML resolver.
    /// </summary>
    /// <returns>A new HTML resolver instance.</returns>
    IHtmlResolver Build();
}
