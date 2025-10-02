using System.Text;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems.RichText;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Resolution;

/// <inheritdoc cref="IHtmlResolver" />
internal sealed class HtmlResolver : IHtmlResolver
{
    private readonly IReadOnlyDictionary<Type, Delegate> _resolvers;
    private readonly HtmlResolverOptions _options;

    public HtmlResolver(
        IReadOnlyDictionary<Type, Delegate> resolvers,
        HtmlResolverOptions options)
    {
        _resolvers = resolvers ?? throw new ArgumentNullException(nameof(resolvers));
        _options = options ?? throw new ArgumentNullException(nameof(options));
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
            IContentItemLink link when _resolvers.TryGetValue(typeof(IContentItemLink), out var resolver)
                => ((BlockResolver<IContentItemLink>)resolver)(link, context, children => ResolveChildrenAsync(children, context)),

            IInlineContentItem item when _resolvers.TryGetValue(typeof(IInlineContentItem), out var resolver)
                => ((BlockResolver<IInlineContentItem>)resolver)(item, context, _ => ValueTask.FromResult(string.Empty)),

            IInlineImage image when _resolvers.TryGetValue(typeof(IInlineImage), out var resolver)
                => ((BlockResolver<IInlineImage>)resolver)(image, context, _ => ValueTask.FromResult(string.Empty)),

            IHtmlElement element when _resolvers.TryGetValue(typeof(IHtmlElement), out var resolver)
                => ((BlockResolver<IHtmlElement>)resolver)(element, context, children => ResolveChildrenAsync(children, context)),

            IHtmlContent html when _resolvers.TryGetValue(typeof(IHtmlContent), out var resolver)
                => ((BlockResolver<IHtmlContent>)resolver)(html, context, _ => ValueTask.FromResult(string.Empty)),

            _ => _options.ThrowOnMissingResolver
                ? throw new InvalidOperationException($"No resolver registered for block type {block.GetType().Name}")
                : ValueTask.FromResult(string.Empty)
        };
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
}
