using System.Text.Json;

namespace Kontent.Ai.Delivery.ContentItems.Processing;

/// <summary>
/// Default implementation of <see cref="IContentDependencyExtractor"/> that extracts
/// cache dependencies from content item elements.
/// </summary>
/// <remarks>
/// <para>
/// This implementation is registered when caching is enabled via
/// <c>AddDeliveryMemoryCache()</c> or <c>AddDeliveryHybridCache()</c> extension methods.
/// </para>
/// <para>
/// All extraction logic for cache dependencies is centralized in this class,
/// keeping the content processors (<see cref="Mapping.ContentItemMapper"/> and
/// <see cref="RichTextParser"/>) focused solely on their primary responsibility
/// of processing content.
/// </para>
/// </remarks>
internal sealed class ContentDependencyExtractor : IContentDependencyExtractor
{
    /// <inheritdoc />
    public void ExtractFromRichTextElement(
        IRichTextElementValue element,
        DependencyTrackingContext? context)
    {
        if (context is null)
        {
            return;
        }

        // Track inline image dependencies
        if (element.Images != null)
        {
            foreach (var imageId in element.Images.Keys)
            {
                context.TrackAsset(imageId);
            }
        }

        // Track content link dependencies
        if (element.Links != null)
        {
            foreach (var link in element.Links.Values)
            {
                context.TrackItem(link.Codename);
            }
        }

        // Track modular content dependencies (inline content items)
        if (element.ModularContent != null)
        {
            foreach (var codename in element.ModularContent)
            {
                context.TrackItem(codename);
            }
        }
    }

    /// <inheritdoc />
    public void ExtractFromTaxonomyElement(
        JsonElement elementValue,
        DependencyTrackingContext? context)
    {
        if (context is null)
        {
            return;
        }

        // Extract taxonomy group codename for dependency tracking
        if (elementValue.TryGetProperty("taxonomy_group", out var taxonomyGroupEl) &&
            taxonomyGroupEl.ValueKind == JsonValueKind.String)
        {
            var taxonomyGroup = taxonomyGroupEl.GetString();
            context.TrackTaxonomy(taxonomyGroup);
        }
    }
}
