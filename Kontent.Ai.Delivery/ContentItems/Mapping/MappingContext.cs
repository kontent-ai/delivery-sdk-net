using System.Text.Json;
using Kontent.Ai.Delivery.Abstractions.ContentItems.Processing;

namespace Kontent.Ai.Delivery.ContentItems.Mapping;

/// <summary>
/// State container for content item mapping operations.
/// Holds the modular content for linked item resolution, dependency tracking context,
/// and cycle detection / memoization state.
/// </summary>
internal sealed class MappingContext
{
    /// <summary>
    /// Raw modular_content from API response for linked item resolution.
    /// </summary>
    public IReadOnlyDictionary<string, JsonElement>? ModularContent { get; init; }

    /// <summary>
    /// Dependency tracking for cache invalidation.
    /// </summary>
    public DependencyTrackingContext? DependencyContext { get; init; }

    /// <summary>
    /// Cycle detection: codenames currently being mapped.
    /// Used to detect and break circular references in linked items.
    /// </summary>
    public HashSet<string> ProcessingItems { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Memoization: already-resolved linked items.
    /// Avoids re-mapping the same linked item multiple times within a request.
    /// </summary>
    public Dictionary<string, object> ResolvedItems { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Cancellation token for async operations.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }
}
