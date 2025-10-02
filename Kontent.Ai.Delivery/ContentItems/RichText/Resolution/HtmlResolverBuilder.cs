using System.Text;
using System.Text.Encodings.Web;
using Kontent.Ai.Delivery.Abstractions;

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
    public IHtmlResolverBuilder WithHtmlContentResolver(BlockResolver<IHtmlContent> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolvers[typeof(IHtmlContent)] = resolver;
        return this;
    }

    /// <inheritdoc />
    public IHtmlResolverBuilder WithHtmlElementResolver(BlockResolver<IHtmlElement> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolvers[typeof(IHtmlElement)] = resolver;
        return this;
    }

    /// <inheritdoc />
    public IHtmlResolverBuilder WithDefaultResolvers()
    {
        // Default pass-through for HTML content (just returns the HTML as-is)
        // Note: Content item links embedded in HTML blocks are not resolved by default
        // Use WithContentItemLinkResolver to handle embedded links if needed
        _resolvers.TryAdd(typeof(IHtmlContent), new BlockResolver<IHtmlContent>(
            (block, _, _) => ValueTask.FromResult(block.Html)
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

                var attributesBuilder = new StringBuilder();
                attributesBuilder.Append($"href=\"{HtmlEncoder.Default.Encode(href)}\"");
                attributesBuilder.Append($" data-item-id=\"{block.ItemId}\"");

                foreach (var (key, value) in block.Attributes)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        attributesBuilder.Append($" {key}=\"{HtmlEncoder.Default.Encode(value)}\"");
                    }
                }

                var attributes = attributesBuilder.ToString();
                return $"<a {attributes}>{innerHtml}</a>";
            }
        ));

        // Default inline content item resolver (returns type name as fallback)
        _resolvers.TryAdd(typeof(IInlineContentItem), new BlockResolver<IInlineContentItem>(
            (block, _, _) => ValueTask.FromResult($"<!-- Inline content item: {block.ContentItem?.GetType().Name ?? "null"} -->")
        ));

        // Default HTML element resolver (renders elements with their structure)
        _resolvers.TryAdd(typeof(IHtmlElement), DefaultResolvers.HtmlElementResolver());

        return this;
    }

    /// <inheritdoc />
    public IHtmlResolver Build() => new HtmlResolver(
        _resolvers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value), // Create defensive copy for thread safety
        _options
    );
}
