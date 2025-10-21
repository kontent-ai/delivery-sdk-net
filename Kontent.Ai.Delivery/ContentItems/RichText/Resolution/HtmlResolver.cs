using System.Collections.Frozen;
using System.Text;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems.RichText;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Resolution;

/// <inheritdoc cref="IHtmlResolver" />
internal sealed class HtmlResolver : IHtmlResolver
{
    private readonly IReadOnlyDictionary<Type, Delegate> _resolvers;
    private readonly HtmlResolverOptions _options;

    // Performance cache: maps tag names to their dedicated resolvers for O(1) lookup
    private readonly FrozenDictionary<string, BlockResolver<IHtmlNode>> _tagResolverCache;

    // Codename-based resolvers for embedded content (components/linked items)
    private readonly FrozenDictionary<string, Func<IEmbeddedContent, IHtmlResolutionContext, ValueTask<string>>> _embeddedContentResolvers;

    // Content-type-specific resolvers for content item links
    private readonly FrozenDictionary<string, BlockResolver<IContentItemLink>> _contentItemLinkResolvers;

    // Diagnostic messages for app-specific resolvers that require configuration
    private const string MissingEmbeddedContentResolver = "<!-- [Kontent.ai SDK] Missing resolver for embedded content of type \"{0}\" (item: {1}, codename: {2}) -->";
    private const string MissingContentItemLinkResolver = "<!-- [Kontent.ai SDK] Missing resolver for link to a content type: \"{0}\" (item ID: {1}) -->";

    public HtmlResolver(
        IReadOnlyDictionary<Type, Delegate> resolvers,
        HtmlResolverOptions options)
    {
        _resolvers = resolvers ?? throw new ArgumentNullException(nameof(resolvers));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        // Build immutable tag resolver cache
        _tagResolverCache = _options.ConditionalHtmlNodeResolvers
            .Where(c => c.Description?.StartsWith("Tag=") == true)
            .ToFrozenDictionary(
                c => c.Description![4..],  // Extract tag name from "Tag=..." description
                c => c.Resolver,
                StringComparer.OrdinalIgnoreCase);

        // Build immutable embedded content resolver cache (codename-based dispatch)
        _embeddedContentResolvers = options.EmbeddedContentResolvers?.ToFrozenDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value,
            StringComparer.OrdinalIgnoreCase) ?? FrozenDictionary<string, Func<IEmbeddedContent, IHtmlResolutionContext, ValueTask<string>>>.Empty;

        // Build immutable content item link resolver cache (content-type-based dispatch)
        _contentItemLinkResolvers = options.ContentItemLinkResolvers?.ToFrozenDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value,
            StringComparer.OrdinalIgnoreCase) ?? FrozenDictionary<string, BlockResolver<IContentItemLink>>.Empty;
    }

    public async ValueTask<string> ResolveAsync(
        IRichTextContent richText,
        IHtmlResolutionContext? context = null)
    {
        if (richText == null)
            throw new ArgumentNullException(nameof(richText));

        context ??= new HtmlResolutionContext();
        var htmlBuilder = new StringBuilder();

        foreach (var block in richText)
        {
            var resolved = await ResolveBlockAsync(block, context);
            htmlBuilder.Append(resolved);
        }

        return htmlBuilder.ToString();
    }

    private ValueTask<string> ResolveBlockAsync(
        IRichTextBlock block,
        IHtmlResolutionContext context)
    {
        return block switch
        {
            IHtmlNode htmlNode => ResolveHtmlNodeAsync(htmlNode, context),

            ITextNode textNode when _resolvers.TryGetValue(typeof(ITextNode), out var resolver)
                => ((BlockResolver<ITextNode>)resolver)(textNode, context, _ => ValueTask.FromResult(string.Empty)),

            IInlineImage image when _resolvers.TryGetValue(typeof(IInlineImage), out var resolver)
                => ((BlockResolver<IInlineImage>)resolver)(image, context, _ => ValueTask.FromResult(string.Empty)),

            // Content item link resolution - type-specific takes precedence
            IContentItemLink link when link.Metadata?.ContentTypeCodename != null
                && _contentItemLinkResolvers.TryGetValue(link.Metadata.ContentTypeCodename, out var typeResolver)
                => typeResolver(link, context, children => ResolveChildrenAsync(children, context)),

            // Fallback to global content item link resolver
            IContentItemLink link when _resolvers.TryGetValue(typeof(IContentItemLink), out var resolver)
                => ((BlockResolver<IContentItemLink>)resolver)(link, context, children => ResolveChildrenAsync(children, context)),

            // No resolver found for content item link
            IContentItemLink link => _options.ThrowOnMissingResolver
                ? throw new InvalidOperationException($"No resolver registered for IContentItemLink (type: {link.Metadata?.ContentTypeCodename ?? "unknown"}, item ID: {link.ItemId})")
                : ValueTask.FromResult(string.Format(MissingContentItemLinkResolver, link.Metadata?.ContentTypeCodename ?? "unknown", link.ItemId)),

            // Embedded content (components/linked items) - codename-based dispatch
            IEmbeddedContent content when _embeddedContentResolvers.TryGetValue(content.ContentTypeCodename, out var resolver)
                => resolver(content, context),

            IEmbeddedContent content => _options.ThrowOnMissingResolver
                ? throw new InvalidOperationException($"No resolver registered for embedded content type: {content.ContentTypeCodename}")
                : ValueTask.FromResult(string.Format(MissingEmbeddedContentResolver, content.ContentTypeCodename, content.Id, content.Codename)),

            _ => throw new InvalidOperationException($"Unknown block type: {block.GetType().Name}")
        };
    }

    private async ValueTask<string> ResolveHtmlNodeAsync(
        IHtmlNode node,
        IHtmlResolutionContext context)
    {
        // Step 1: Check tag resolver cache for O(1) lookup
        if (_tagResolverCache.TryGetValue(node.TagName, out var cachedResolver))
        {
            return await cachedResolver(node, context, children => ResolveChildrenAsync(children, context));
        }

        // Step 2: Evaluate conditional resolvers in registration order
        foreach (var conditional in _options.ConditionalHtmlNodeResolvers)
        {
            if (conditional.Predicate(node))
            {
                return await conditional.Resolver(node, context, children => ResolveChildrenAsync(children, context));
            }
        }

        // Step 3: Use default HTML node resolver if configured
        if (_options.DefaultHtmlNodeResolver != null)
        {
            return await _options.DefaultHtmlNodeResolver(node, context, children => ResolveChildrenAsync(children, context));
        }

        // Step 4: Ultimate fallback - built-in default
        return await DefaultResolvers.HtmlElementResolver()(node, context, children => ResolveChildrenAsync(children, context));
    }

    private async ValueTask<string> ResolveChildrenAsync(
        IEnumerable<IRichTextBlock> children,
        IHtmlResolutionContext context)
    {
        var builder = new StringBuilder();
        foreach (var child in children)
        {
            builder.Append(await ResolveBlockAsync(child, context));
        }
        return builder.ToString();
    }
}

/// <summary>
/// Options for configuring HTML resolver behavior.
/// </summary>
internal sealed record HtmlResolverOptions
{
    /// <summary>
    /// When true, throws an exception if embedded content or content item link have no registered resolver.
    /// When false, silently skips blocks without resolvers.
    /// Default: false.
    /// </summary>
    public bool ThrowOnMissingResolver { get; init; } = false;

    /// <summary>
    /// Ordered list of conditional HTML node resolvers.
    /// Evaluated in order - first matching predicate wins.
    /// </summary>
    public IReadOnlyList<ConditionalHtmlNodeResolver> ConditionalHtmlNodeResolvers { get; init; } = [];

    /// <summary>
    /// Fallback resolver for HTML nodes when no conditional resolver matches.
    /// </summary>
    public BlockResolver<IHtmlNode>? DefaultHtmlNodeResolver { get; init; }

    /// <summary>
    /// Codename-based resolvers for embedded content (components and linked items).
    /// Key is the content type codename, value is the resolver function.
    /// </summary>
    public IReadOnlyDictionary<string, Func<IEmbeddedContent, IHtmlResolutionContext, ValueTask<string>>>? EmbeddedContentResolvers { get; init; }

    /// <summary>
    /// Content-type-specific resolvers for content item links.
    /// Key is the content type codename, value is the resolver function.
    /// </summary>
    public IReadOnlyDictionary<string, BlockResolver<IContentItemLink>>? ContentItemLinkResolvers { get; init; }
}
