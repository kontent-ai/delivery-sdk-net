using System;
using System.Text.Json;
using Kontent.Ai.Delivery.Abstractions.ContentItems.Processing;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Extracts cache dependencies from content item elements during processing.
/// This interface enables separation of concerns between content processing and caching logic.
/// </summary>
/// <remarks>
/// <para>
/// Implementations analyze element values to identify dependencies on assets, taxonomies,
/// and linked items, which are tracked in a <see cref="DependencyTrackingContext"/> for cache invalidation.
/// </para>
/// <para>
/// A no-op implementation is used when caching is disabled.
/// </para>
/// </remarks>
internal interface IContentDependencyExtractor
{
    /// <summary>
    /// Extracts dependencies from a rich text element value.
    /// </summary>
    /// <param name="element">The rich text element value containing images, links, and modular content.</param>
    /// <param name="context">
    /// Optional tracking context to populate with dependencies.
    /// When null, no extraction occurs (caching disabled).
    /// </param>
    /// <remarks>
    /// Tracks dependencies on:
    /// <list type="bullet">
    /// <item><description>Inline images (from Images dictionary)</description></item>
    /// <item><description>Content item links (from Links dictionary)</description></item>
    /// <item><description>Inline content items (from ModularContent array)</description></item>
    /// </list>
    /// </remarks>
    void ExtractFromRichTextElement(
        IRichTextElementValue element,
        DependencyTrackingContext? context);

    /// <summary>
    /// Extracts dependencies from a taxonomy element.
    /// </summary>
    /// <param name="elementValue">The raw JSON element containing taxonomy data.</param>
    /// <param name="context">
    /// Optional tracking context to populate with dependencies.
    /// When null, no extraction occurs (caching disabled).
    /// </param>
    /// <remarks>
    /// Tracks a dependency on the taxonomy group (not individual terms).
    /// The taxonomy group codename is extracted from the "taxonomy_group" JSON property.
    /// </remarks>
    void ExtractFromTaxonomyElement(
        JsonElement elementValue,
        DependencyTrackingContext? context);
}
