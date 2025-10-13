using System.Text;
using System.Text.Encodings.Web;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Resolution;

/// <summary>
/// Provides pre-built resolvers for common rich text block resolution scenarios.
/// </summary>
public static class DefaultResolvers
{
    /// <summary>
    /// Creates a content item link resolver that generates URLs based on a pattern string.
    /// Placeholders: {codename}, {type}, {urlslug}, {id}
    /// </summary>
    /// <param name="pattern">The URL pattern with placeholders.</param>
    /// <returns>A block resolver for content item links.</returns>
    /// <example>
    /// <code>
    /// var resolver = DefaultResolvers.UrlPatternResolver("/articles/{urlslug}");
    /// </code>
    /// </example>
    public static BlockResolver<IContentItemLink> UrlPatternResolver(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        return async (block, context, resolveChildren) =>
        {
            var innerHtml = await resolveChildren(block.Children);

            var url = pattern
                .Replace("{codename}", block.Metadata?.Codename ?? string.Empty)
                .Replace("{type}", block.Metadata?.ContentTypeCodename ?? string.Empty)
                .Replace("{urlslug}", block.Metadata?.UrlSlug ?? string.Empty)
                .Replace("{id}", block.ItemId.ToString());

            var attributes = BuildAttributes(block.Attributes, ("href", url), ("data-item-id", block.ItemId.ToString()));
            return $"<a {attributes}>{innerHtml}</a>";
        };
    }

    /// <summary>
    /// Creates a content item link resolver that uses the legacy IContentLinkUrlResolver.
    /// Provided for backward compatibility when migrating from string-based resolution.
    /// </summary>
    /// <param name="urlResolver">The legacy URL resolver.</param>
    /// <returns>A block resolver for content item links.</returns>
    public static BlockResolver<IContentItemLink> LegacyUrlResolver(IContentLinkUrlResolver urlResolver)
    {
        ArgumentNullException.ThrowIfNull(urlResolver);

        return async (block, context, resolveChildren) =>
        {
            var innerHtml = await resolveChildren(block.Children);

            string url;
            if (block.Metadata != null)
            {
                url = await urlResolver.ResolveLinkUrlAsync(block.Metadata);
            }
            else
            {
                url = await urlResolver.ResolveBrokenLinkUrlAsync();
            }

            if (string.IsNullOrEmpty(url))
            {
                // If resolver returns null/empty, render link without href (broken link)
                var brokenAttributes = BuildAttributes(block.Attributes, ("data-item-id", block.ItemId.ToString()));
                return $"<a {brokenAttributes}>{innerHtml}</a>";
            }

            var attributes = BuildAttributes(block.Attributes, ("href", url), ("data-item-id", block.ItemId.ToString()));
            return $"<a {attributes}>{innerHtml}</a>";
        };
    }

    /// <summary>
    /// Creates a simple pass-through resolver for inline content items that renders them as comments.
    /// Useful for scenarios where inline content should not be displayed but preserved for debugging.
    /// </summary>
    /// <returns>A block resolver for inline content items.</returns>
    public static BlockResolver<IInlineContentItem> CommentResolver()
    {
        return (block, _, _) =>
        {
            // Try to extract codename from common patterns without using dynamic
            var codename = block.ContentItem?.GetType().Name ?? "unknown";

            // Attempt to get codename from System property if available
            var systemProperty = block.ContentItem?.GetType().GetProperty("System");
            if (systemProperty != null)
            {
                var systemValue = systemProperty.GetValue(block.ContentItem);
                var codenameProperty = systemValue?.GetType().GetProperty("Codename");
                if (codenameProperty != null)
                {
                    codename = codenameProperty.GetValue(systemValue) as string ?? codename;
                }
            }

            return ValueTask.FromResult($"<!-- Inline content item: {codename} -->");
        };
    }

    /// <summary>
    /// Creates a default resolver for HTML elements that renders them with their original structure.
    /// </summary>
    /// <returns>A block resolver for HTML elements.</returns>
    public static BlockResolver<IHtmlNode> HtmlElementResolver()
    {
        return async (block, context, resolveChildren) =>
        {
            var children = await resolveChildren(block.Children);
            var attributes = BuildAttributes(block.Attributes);

            var attributesStr = string.IsNullOrEmpty(attributes) ? "" : $" {attributes}";

            // Self-closing tags
            var voidElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "area", "base", "br", "col", "embed", "hr", "img", "input",
                "link", "meta", "param", "source", "track", "wbr"
            };

            if (voidElements.Contains(block.TagName))
            {
                return $"<{block.TagName}{attributesStr}>";
            }

            return $"<{block.TagName}{attributesStr}>{children}</{block.TagName}>";
        };
    }

    /// <summary>
    /// Builds an HTML attribute string from a dictionary and optional additional attributes.
    /// </summary>
    /// <param name="existingAttributes">Existing attributes from the block.</param>
    /// <param name="additional">Additional attributes to include.</param>
    /// <returns>A space-separated string of HTML attributes.</returns>
    private static string BuildAttributes(
        IReadOnlyDictionary<string, string> existingAttributes,
        params (string key, string value)[] additional)
    {
        var allAttributes = existingAttributes
            .Concat(additional.Select(a => new KeyValuePair<string, string>(a.key, a.value)))
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
            .Select(kvp => $"{kvp.Key}=\"{HtmlEncoder.Default.Encode(kvp.Value)}\"");

        return string.Join(" ", allAttributes);
    }

    /// <summary>
    /// Overload for building attributes without additional ones.
    /// </summary>
    private static string BuildAttributes(IReadOnlyDictionary<string, string> existingAttributes)
    {
        return BuildAttributes(existingAttributes, Array.Empty<(string, string)>());
    }
}
