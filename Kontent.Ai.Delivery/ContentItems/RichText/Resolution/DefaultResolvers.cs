using System.Text;
using System.Text.Encodings.Web;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Resolution;

/// <summary>
/// Provides pre-built resolvers for common rich text block resolution scenarios.
/// </summary>
public static class DefaultResolvers
{
    /// <summary>
    /// Creates a content item link resolver that generates URLs based on content type-specific patterns.
    /// Each content type can have its own URL pattern. Placeholders: {codename}, {type}, {urlslug}, {id}
    /// </summary>
    /// <param name="typePatterns">Dictionary mapping content type codenames to URL patterns.</param>
    /// <param name="fallbackPattern">Optional fallback pattern for content types not in the dictionary. If null, uses "/content/{id}".</param>
    /// <returns>A block resolver for content item links.</returns>
    /// <example>
    /// <code>
    /// var resolver = DefaultResolvers.UrlPatternResolver(new Dictionary&lt;string, string&gt;
    /// {
    ///     ["article"] = "/articles/{urlslug}",
    ///     ["product"] = "/shop/products/{urlslug}",
    ///     ["author"] = "/about/authors/{codename}"
    /// });
    /// </code>
    /// </example>
    public static BlockResolver<IContentItemLink> UrlPatternResolver(
        IReadOnlyDictionary<string, string> typePatterns,
        string? fallbackPattern = null)
    {
        ArgumentNullException.ThrowIfNull(typePatterns);

        return async (block, context, resolveChildren) =>
        {
            var innerHtml = await resolveChildren(block.Children);

            // Get pattern for this content type, or use fallback
            var contentType = block.Metadata?.ContentTypeCodename ?? string.Empty;
            var pattern = typePatterns.TryGetValue(contentType, out var typePattern)
                ? typePattern
                : fallbackPattern ?? "/content/{id}";

            // Apply pattern with placeholder replacement
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
