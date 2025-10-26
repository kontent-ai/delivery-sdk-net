using System.Text.Encodings.Web;
using System.Text.Unicode;

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
///   <item><description><see cref="IEmbeddedContent"/> - Embedded content items/components (requires rendering logic)</description></item>
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
///     .WithContentResolver("tweet", (content, ctx) =>
///         $"&lt;div class='tweet'&gt;{content.Name}&lt;/div&gt;")
///     .WithContentResolver("video", (content, ctx) =>
///         $"&lt;video src='{content.Content.Url}'&gt;&lt;/video&gt;")
///     .Build();
///
/// var html = await richText.ToHtmlAsync(resolver);
/// </code>
/// </example>
public sealed class HtmlResolverBuilder : IHtmlResolverBuilder
{
    private readonly Dictionary<Type, Delegate> _resolvers = [];
    private readonly List<ConditionalHtmlNodeResolver> _conditionalHtmlNodeResolvers = [];
    private readonly Dictionary<string, Func<IEmbeddedContent, ValueTask<string>>> _embeddedContentResolvers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, BlockResolver<IContentItemLink>> _contentItemLinkResolvers = new(StringComparer.OrdinalIgnoreCase);
    private readonly HtmlResolverOptions _options = new();

    /// <inheritdoc />
    public IHtmlResolverBuilder WithContentItemLinkResolver(BlockResolver<IContentItemLink> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolvers[typeof(IContentItemLink)] = resolver;
        return this;
    }

    /// <inheritdoc />
    public IHtmlResolverBuilder WithContentItemLinkResolver(
        string contentTypeCodename,
        BlockResolver<IContentItemLink> resolver)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentTypeCodename);
        ArgumentNullException.ThrowIfNull(resolver);

        _contentItemLinkResolvers[contentTypeCodename] = resolver;
        return this;
    }

    /// <inheritdoc />
    public IHtmlResolverBuilder WithContentItemLinkResolvers(
        IReadOnlyDictionary<string, BlockResolver<IContentItemLink>> resolvers)
    {
        ArgumentNullException.ThrowIfNull(resolvers);

        foreach (var (codename, resolver) in resolvers)
        {
            WithContentItemLinkResolver(codename, resolver);
        }
        return this;
    }

    /// <inheritdoc />
    public IHtmlResolverBuilder WithContentItemLinkResolvers(
        params (string ContentTypeCodename, BlockResolver<IContentItemLink> Resolver)[] resolvers)
    {
        ArgumentNullException.ThrowIfNull(resolvers);

        foreach (var (codename, resolver) in resolvers)
        {
            WithContentItemLinkResolver(codename, resolver);
        }
        return this;
    }

    /// <inheritdoc />
    public IHtmlResolverBuilder WithContentResolver(
        string contentTypeCodename,
        Func<IEmbeddedContent, ValueTask<string>> resolver)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentTypeCodename);
        ArgumentNullException.ThrowIfNull(resolver);

        _embeddedContentResolvers[contentTypeCodename] = resolver;
        return this;
    }

    /// <inheritdoc />
    public IHtmlResolverBuilder WithContentResolver(
        string contentTypeCodename,
        Func<IEmbeddedContent, string> resolver)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentTypeCodename);
        ArgumentNullException.ThrowIfNull(resolver);

        // Wrap synchronous resolver in ValueTask
        _embeddedContentResolvers[contentTypeCodename] = content =>
            ValueTask.FromResult(resolver(content));
        return this;
    }

    /// <inheritdoc />
    public IHtmlResolverBuilder WithContentResolvers(
        IReadOnlyDictionary<string, Func<IEmbeddedContent, string>> resolvers)
    {
        ArgumentNullException.ThrowIfNull(resolvers);

        foreach (var (codename, resolver) in resolvers)
        {
            WithContentResolver(codename, resolver);
        }
        return this;
    }

    /// <inheritdoc />
    public IHtmlResolverBuilder WithContentResolvers(
        params (string ContentTypeCodename, Func<IEmbeddedContent, string> Resolver)[] resolvers)
    {
        ArgumentNullException.ThrowIfNull(resolvers);

        foreach (var (codename, resolver) in resolvers)
        {
            WithContentResolver(codename, resolver);
        }
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
    public IHtmlResolver Build() // TODO: ensure resolver handles missing linked item (depth = 0) gracefully
    {
        // Always provide built-in defaults for elements that have sensible default rendering
        var resolversWithDefaults = new Dictionary<Type, Delegate>(_resolvers);

        // Create HTML encoder that preserves Unicode characters (emojis, smart quotes, accented chars)
        // but still encodes HTML-reserved characters (<, >, &, ", ') for security
        var unicodeEncoder = HtmlEncoder.Create(UnicodeRanges.All);

        // Default text node resolver - HTML-encodes reserved chars but preserves Unicode
        resolversWithDefaults.TryAdd(typeof(ITextNode), new BlockResolver<ITextNode>(
            (block, _) => ValueTask.FromResult(unicodeEncoder.Encode(block.Text))
        ));

        // Default inline image resolver - generates proper HTML figure element
        resolversWithDefaults.TryAdd(typeof(IInlineImage), new BlockResolver<IInlineImage>(
            (block, _) =>
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

        // Note: IContentItemLink and IEmbeddedContent have NO defaults
        // They require explicit configuration and will show diagnostic comments if missing

        // Create options with conditional resolvers, embedded content resolvers, content item link resolvers, and default fallback
        var options = new HtmlResolverOptions
        {
            ConditionalHtmlNodeResolvers = [.. _conditionalHtmlNodeResolvers],
            DefaultHtmlNodeResolver = defaultHtmlNodeResolver,
            ThrowOnMissingResolver = _options.ThrowOnMissingResolver,
            EmbeddedContentResolvers = _embeddedContentResolvers.Count > 0 ? _embeddedContentResolvers : null,
            ContentItemLinkResolvers = _contentItemLinkResolvers.Count > 0 ? _contentItemLinkResolvers : null
        };

        // Create resolver dictionary excluding IHtmlNode (handled via options)
        var resolverDict = resolversWithDefaults
            .Where(kvp => kvp.Key != typeof(IHtmlNode))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return new HtmlResolver(resolverDict, options);
    }
}
