using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Builder interface for fluent configuration of HTML resolvers.
/// </summary>
public interface IHtmlResolverBuilder
{
    /// <summary>
    /// Registers a global resolver for all content item link blocks.
    /// This resolver will be used for all content item links unless a type-specific resolver is registered.
    /// </summary>
    /// <param name="resolver">The resolver function.</param>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithContentItemLinkResolver(BlockResolver<IContentItemLink> resolver);

    /// <summary>
    /// Registers a resolver for content item link blocks of a specific content type.
    /// Type-specific resolvers take precedence over the global resolver.
    /// </summary>
    /// <param name="contentTypeCodename">The codename of the content type to resolve (e.g., "article", "product").</param>
    /// <param name="resolver">The resolver function for this content type.</param>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithContentItemLinkResolver(
        string contentTypeCodename,
        BlockResolver<IContentItemLink> resolver);

    /// <summary>
    /// Registers multiple resolvers for content item link blocks using a dictionary.
    /// </summary>
    /// <param name="resolvers">Dictionary mapping content type codenames to their resolver functions.</param>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithContentItemLinkResolvers(
        IReadOnlyDictionary<string, BlockResolver<IContentItemLink>> resolvers);

    /// <summary>
    /// Registers multiple resolvers for content item link blocks using tuples.
    /// </summary>
    /// <param name="resolvers">Tuples of (content type codename, resolver function).</param>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithContentItemLinkResolvers(
        params (string ContentTypeCodename, BlockResolver<IContentItemLink> Resolver)[] resolvers);

    /// <summary>
    /// Registers an async resolver for embedded content (components or linked items) of a specific content type.
    /// </summary>
    /// <param name="contentTypeCodename">The codename of the content type to resolve (e.g., "article", "tweet").</param>
    /// <param name="resolver">The async resolver function that returns HTML for the content.</param>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithContentResolver(
        string contentTypeCodename,
        Func<IEmbeddedContent, ValueTask<string>> resolver);

    /// <summary>
    /// Registers a synchronous resolver for embedded content (components or linked items) of a specific content type.
    /// </summary>
    /// <param name="contentTypeCodename">The codename of the content type to resolve (e.g., "article", "tweet").</param>
    /// <param name="resolver">The synchronous resolver function that returns HTML for the content.</param>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithContentResolver(
        string contentTypeCodename,
        Func<IEmbeddedContent, string> resolver);

    /// <summary>
    /// Registers multiple resolvers for embedded content using a dictionary.
    /// </summary>
    /// <param name="resolvers">Dictionary mapping content type codenames to their resolver functions.</param>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithContentResolvers(
        IReadOnlyDictionary<string, Func<IEmbeddedContent, string>> resolvers);

    /// <summary>
    /// Registers multiple resolvers for embedded content using tuples.
    /// </summary>
    /// <param name="resolvers">Tuples of (content type codename, resolver function).</param>
    /// <returns>This builder for method chaining.</returns>
    IHtmlResolverBuilder WithContentResolvers(
        params (string ContentTypeCodename, Func<IEmbeddedContent, string> Resolver)[] resolvers);

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