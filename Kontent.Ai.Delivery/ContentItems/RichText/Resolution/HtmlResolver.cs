using System.Text;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Abstractions.ContentItems.RichText.Blocks;
using Kontent.Ai.Delivery.ContentItems.RichText;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Resolution;

/// <inheritdoc cref="IHtmlResolver" />
internal sealed class HtmlResolver : IHtmlResolver
{
    private readonly IReadOnlyDictionary<Type, Delegate> _resolvers;
    private readonly HtmlResolverOptions _options;

    // Performance cache: maps tag names to their dedicated resolvers for O(1) lookup
    private readonly Dictionary<string, BlockResolver<IHtmlNode>?> _tagResolverCache;

    public HtmlResolver(
        IReadOnlyDictionary<Type, Delegate> resolvers,
        HtmlResolverOptions options)
    {
        _resolvers = resolvers ?? throw new ArgumentNullException(nameof(resolvers));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        // Pre-populate cache for simple tag-based resolvers
        _tagResolverCache = new Dictionary<string, BlockResolver<IHtmlNode>?>(StringComparer.OrdinalIgnoreCase);
        foreach (var conditional in _options.ConditionalHtmlNodeResolvers)
        {
            // Only cache simple tag matchers for performance
            if (conditional.Description?.StartsWith("Tag=") == true)
            {
                var tagName = conditional.Description[4..];
                _tagResolverCache.TryAdd(tagName, conditional.Resolver);
            }
        }
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
            // Special handling for IHtmlNode with conditional resolution
            IHtmlNode htmlNode => ResolveHtmlNodeAsync(htmlNode, context),

            // ITextNode resolution - encode and return text
            ITextNode textNode when _resolvers.TryGetValue(typeof(ITextNode), out var resolver)
                => ((BlockResolver<ITextNode>)resolver)(textNode, context, _ => ValueTask.FromResult(string.Empty)),
            ITextNode textNode
                => ValueTask.FromResult(System.Text.Encodings.Web.HtmlEncoder.Default.Encode(textNode.Text)),

            IContentItemLink link when _resolvers.TryGetValue(typeof(IContentItemLink), out var resolver)
                => ((BlockResolver<IContentItemLink>)resolver)(link, context, children => ResolveChildrenAsync(children, context)),

            IInlineContentItem item when _resolvers.TryGetValue(typeof(IInlineContentItem), out var resolver)
                => ((BlockResolver<IInlineContentItem>)resolver)(item, context, _ => ValueTask.FromResult(string.Empty)),

            IInlineImage image when _resolvers.TryGetValue(typeof(IInlineImage), out var resolver)
                => ((BlockResolver<IInlineImage>)resolver)(image, context, _ => ValueTask.FromResult(string.Empty)),

            _ => _options.ThrowOnMissingResolver
                ? throw new InvalidOperationException($"No resolver registered for block type {block.GetType().Name}")
                : ValueTask.FromResult(string.Empty)
        };
    }

    private async ValueTask<string> ResolveHtmlNodeAsync(
        IHtmlNode node,
        IHtmlResolutionContext context)
    {
        // Step 1: Check tag resolver cache for O(1) lookup
        if (_tagResolverCache.TryGetValue(node.TagName, out var cachedResolver) && cachedResolver != null)
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
    /// When true, throws an exception if a block type has no registered resolver.
    /// When false, silently skips blocks without resolvers.
    /// Default: false.
    /// </summary>
    public bool ThrowOnMissingResolver { get; init; } = false;

    /// <summary>
    /// Ordered list of conditional HTML node resolvers.
    /// Evaluated in order - first matching predicate wins.
    /// </summary>
    public IReadOnlyList<ConditionalHtmlNodeResolver> ConditionalHtmlNodeResolvers { get; init; }
        = Array.Empty<ConditionalHtmlNodeResolver>();

    /// <summary>
    /// Fallback resolver for HTML nodes when no conditional resolver matches.
    /// </summary>
    public BlockResolver<IHtmlNode>? DefaultHtmlNodeResolver { get; init; }
}
