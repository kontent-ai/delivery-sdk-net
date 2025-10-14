using System.Text;
using System.Text.Encodings.Web;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Abstractions.ContentItems.RichText.Blocks;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Resolution;

/// <summary>
/// Fluent builder for configuring HTML resolvers for rich text content.
/// </summary>
/// <remarks>
/// This builder allows you to register custom resolvers for different block types
/// and configure how rich text content is transformed into HTML strings.
/// Use <see cref="WithDefaultResolvers"/> to register sensible defaults for all block types,
/// then override specific resolvers as needed.
/// </remarks>
/// <example>
/// <code>
/// var resolver = new HtmlResolverBuilder()
///     .WithDefaultResolvers()
///     .WithContentItemLinkResolver(DefaultResolvers.UrlPatternResolver("/articles/{urlslug}"))
///     .Build();
///
/// var html = await richText.ToHtmlAsync(resolver);
/// </code>
/// </example>
public sealed class HtmlResolverBuilder : IHtmlResolverBuilder
{
    private readonly Dictionary<Type, Delegate> _resolvers = new();
    private readonly List<ConditionalHtmlNodeResolver> _conditionalHtmlNodeResolvers = new();
    private readonly HtmlResolverOptions _options = new();

    /// <inheritdoc />
    public IHtmlResolverBuilder WithContentItemLinkResolver(BlockResolver<IContentItemLink> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolvers[typeof(IContentItemLink)] = resolver;
        return this;
    }

    /// <inheritdoc />
    public IHtmlResolverBuilder WithInlineContentItemResolver(BlockResolver<IInlineContentItem> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolvers[typeof(IInlineContentItem)] = resolver;
        return this;
    }

    /// <inheritdoc />
    public IHtmlResolverBuilder WithInlineImageResolver(BlockResolver<IInlineImage> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolvers[typeof(IInlineImage)] = resolver;
        return this;
    }

    /// <inheritdoc />
    public IHtmlResolverBuilder WithTextNodeResolver(BlockResolver<ITextNode> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolvers[typeof(ITextNode)] = resolver;
        return this;
    }

    /// <inheritdoc />
    public IHtmlResolverBuilder WithHtmlNodeResolver(
        HtmlNodePredicate predicate,
        BlockResolver<IHtmlNode> resolver,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(resolver);

        _conditionalHtmlNodeResolvers.Add(new ConditionalHtmlNodeResolver(
            predicate,
            resolver,
            description));

        return this;
    }

    /// <inheritdoc />
    public IHtmlResolverBuilder WithHtmlNodeResolver(
        string tagName,
        BlockResolver<IHtmlNode> resolver)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
        ArgumentNullException.ThrowIfNull(resolver);

        return WithHtmlNodeResolver(
            node => node.TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase),
            resolver,
            $"Tag={tagName}");
    }

    /// <inheritdoc />
    public IHtmlResolverBuilder WithHtmlNodeResolverForAttribute(
        string attributeName,
        string? attributeValue,
        BlockResolver<IHtmlNode> resolver)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attributeName);
        ArgumentNullException.ThrowIfNull(resolver);

        HtmlNodePredicate predicate = attributeValue == null
            ? node => node.Attributes.ContainsKey(attributeName)
            : node => node.Attributes.TryGetValue(attributeName, out var value)
                   && value?.Equals(attributeValue, StringComparison.OrdinalIgnoreCase) == true;

        var description = attributeValue == null
            ? $"Attribute={attributeName}"
            : $"Attribute={attributeName}[{attributeValue}]";

        return WithHtmlNodeResolver(predicate, resolver, description);
    }

    /// <inheritdoc />
    public IHtmlResolverBuilder WithHtmlElementResolver(BlockResolver<IHtmlNode> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolvers[typeof(IHtmlNode)] = resolver;
        return this;
    }

    /// <inheritdoc />
    public IHtmlResolverBuilder WithDefaultResolvers()
    {
        // Default text node resolver - HTML-encodes text content
        _resolvers.TryAdd(typeof(ITextNode), new BlockResolver<ITextNode>(
            (block, _, _) => ValueTask.FromResult(HtmlEncoder.Default.Encode(block.Text))
        ));

        // Default inline image resolver - generates proper HTML figure element
        _resolvers.TryAdd(typeof(IInlineImage), new BlockResolver<IInlineImage>(
            (block, _, _) =>
            {
                var url = HtmlEncoder.Default.Encode(block.Url ?? string.Empty);
                var description = HtmlEncoder.Default.Encode(block.Description ?? string.Empty);
                var html = $"<figure><img src=\"{url}\" alt=\"{description}\" data-asset-id=\"{block.ImageId}\" /></figure>";
                return ValueTask.FromResult(html);
            }
        ));

        // Default content item link resolver (renders with href from UrlSlug or empty)
        _resolvers.TryAdd(typeof(IContentItemLink), new BlockResolver<IContentItemLink>(
            async (block, context, resolveChildren) =>
            {
                var innerHtml = await resolveChildren(block.Children);
                var href = block.Metadata?.UrlSlug ?? string.Empty;

                string[] baseAttributes = [
                    $"href=\"{HtmlEncoder.Default.Encode(href)}\"",
                    $"data-item-id=\"{block.ItemId}\""
                ];

                var customAttributes = block.Attributes
                    .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                    .Select(kvp => $"{kvp.Key}=\"{HtmlEncoder.Default.Encode(kvp.Value)}\"");

                var attributes = string.Join(" ", baseAttributes.Concat(customAttributes));
                return $"<a {attributes}>{innerHtml}</a>";
            }
        ));

        // Default inline content item resolver (returns type name as fallback)
        _resolvers.TryAdd(typeof(IInlineContentItem), new BlockResolver<IInlineContentItem>(
            (block, _, _) => ValueTask.FromResult($"<!-- Inline content item: {block.ContentItem?.GetType().Name ?? "null"} -->")
        ));

        // Default HTML element resolver (renders elements with their structure)
        _resolvers.TryAdd(typeof(IHtmlNode), DefaultResolvers.HtmlElementResolver());

        return this;
    }

    /// <inheritdoc />
    public IHtmlResolver Build()
    {
        // Extract the default HTML node resolver if registered
        var defaultHtmlNodeResolver = _resolvers.TryGetValue(typeof(IHtmlNode), out var resolver)
            ? (BlockResolver<IHtmlNode>)resolver
            : null;

        // Create options with conditional resolvers and default fallback
        var options = new HtmlResolverOptions
        {
            ConditionalHtmlNodeResolvers = _conditionalHtmlNodeResolvers.ToArray(),
            DefaultHtmlNodeResolver = defaultHtmlNodeResolver,
            ThrowOnMissingResolver = _options.ThrowOnMissingResolver
        };

        // Create resolver dictionary excluding IHtmlNode (handled via options)
        var resolverDict = _resolvers
            .Where(kvp => kvp.Key != typeof(IHtmlNode))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return new HtmlResolver(resolverDict, options);
    }
}
