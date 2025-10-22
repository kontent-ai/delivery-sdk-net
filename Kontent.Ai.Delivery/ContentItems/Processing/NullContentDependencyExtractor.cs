using System.Text.Json;
using Kontent.Ai.Delivery.Abstractions.ContentItems.Processing;

namespace Kontent.Ai.Delivery.ContentItems.Processing;

/// <summary>
/// No-op implementation of <see cref="IContentDependencyExtractor"/> used when caching is disabled.
/// </summary>
/// <remarks>
/// <para>
/// This implementation is registered as the default when the Delivery Client is configured
/// without caching. All methods are empty and have zero overhead.
/// </para>
/// <para>
/// Implemented as a singleton to avoid unnecessary allocations. The JIT compiler will
/// optimize away the method calls since they have no side effects.
/// </para>
/// </remarks>
internal sealed class NullContentDependencyExtractor : IContentDependencyExtractor
{
    /// <summary>
    /// Gets the singleton instance of the null extractor.
    /// </summary>
    public static NullContentDependencyExtractor Instance { get; } = new();

    /// <summary>
    /// Private constructor to enforce singleton pattern.
    /// </summary>
    private NullContentDependencyExtractor()
    {
    }

    /// <inheritdoc />
    /// <remarks>This method performs no operations and returns immediately.</remarks>
    public void ExtractFromRichTextElement(
        IRichTextElementValue element,
        DependencyTrackingContext? context)
    {
        // No-op: Caching is disabled
    }

    /// <inheritdoc />
    /// <remarks>This method performs no operations and returns immediately.</remarks>
    public void ExtractFromTaxonomyElement(
        JsonElement elementValue,
        DependencyTrackingContext? context)
    {
        // No-op: Caching is disabled
    }
}