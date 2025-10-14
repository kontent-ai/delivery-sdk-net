using System.Text;
using System.Text.Encodings.Web;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Abstractions.ContentItems.RichText.Blocks;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Resolution;

/// <summary>
/// Fluent builder for configuring HTML resolvers for rich text content.
/// </summary>
/// <remarks>
/// <para>
/// The SDK automatically provides sensible defaults for text nodes, HTML elements, and inline images.
/// You only need to configure resolvers for app-specific content:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="IContentItemLink"/> - Links to other content items (requires URL generation logic)</description></item>
///   <item><description><see cref="IInlineContentItem"/> - Embedded content items (requires rendering logic)</description></item>
/// </list>
/// <para>
/// Custom resolvers override the built-in defaults.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var resolver = new HtmlResolverBuilder()
///     .WithContentItemLinkResolver(DefaultResolvers.UrlPatternResolver(new Dictionary&lt;string, string&gt;
///     {
///         ["article"] = "/articles/{urlslug}",
///         ["product"] = "/products/{urlslug}"
///     }))
///     .WithInlineContentItemResolver((item, ctx, _) => ValueTask.FromResult($"&lt;div class='embed'&gt;{item.ContentItem.System.Name}&lt;/div&gt;"))
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
    public IHtmlResolver Build()
    {
        // Always provide built-in defaults for elements that have sensible default rendering
        var resolversWithDefaults = new Dictionary<Type, Delegate>(_resolvers);

        // Default text node resolver - HTML-encodes text content
        resolversWithDefaults.TryAdd(typeof(ITextNode), new BlockResolver<ITextNode>(
            (block, _, _) => ValueTask.FromResult(HtmlEncoder.Default.Encode(block.Text))
        ));

        // Default inline image resolver - generates proper HTML figure element
        resolversWithDefaults.TryAdd(typeof(IInlineImage), new BlockResolver<IInlineImage>(
            (block, _, _) =>
            {
                var url = HtmlEncoder.Default.Encode(block.Url ?? string.Empty);
                var description = HtmlEncoder.Default.Encode(block.Description ?? string.Empty);
                var html = $"<figure><img src=\"{url}\" alt=\"{description}\" data-asset-id=\"{block.ImageId}\" /></figure>";
                return ValueTask.FromResult(html);
            }
        ));

        // Default HTML element resolver - renders elements with their structure
        var defaultHtmlNodeResolver = resolversWithDefaults.TryGetValue(typeof(IHtmlNode), out var htmlResolver)
            ? (BlockResolver<IHtmlNode>)htmlResolver
            : DefaultResolvers.HtmlElementResolver();

        // Note: IContentItemLink and IInlineContentItem have NO defaults
        // They require explicit configuration and will show diagnostic comments if missing

        // Create options with conditional resolvers and default fallback
        var options = new HtmlResolverOptions
        {
            ConditionalHtmlNodeResolvers = _conditionalHtmlNodeResolvers.ToArray(),
            DefaultHtmlNodeResolver = defaultHtmlNodeResolver,
            ThrowOnMissingResolver = _options.ThrowOnMissingResolver
        };

        // Create resolver dictionary excluding IHtmlNode (handled via options)
        var resolverDict = resolversWithDefaults
            .Where(kvp => kvp.Key != typeof(IHtmlNode))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return new HtmlResolver(resolverDict, options);
    }
}
