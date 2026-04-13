namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Tracks cache dependencies discovered during content item processing.
/// Used internally to build the set of dependency keys that should invalidate
/// a cached API response when the referenced content changes.
/// </summary>
/// <remarks>
/// <para>
/// This class is designed to be used during a single API response processing pipeline.
/// As elements are hydrated (rich text, taxonomies, linked items), dependencies are
/// tracked by calling <see cref="TrackItem"/>, <see cref="TrackAsset"/>, or <see cref="TrackTaxonomy"/>.
/// </para>
/// <para>
/// Thread-safety: This class is thread-safe and can be safely accessed from multiple
/// concurrent operations during async processing. However, it's typically used within
/// a single request context where such concurrency is unlikely.
/// </para>
/// <para>
/// Dependency key formats:
/// <list type="bullet">
/// <item><description>Content items: <c>item_{codename}</c></description></item>
/// <item><description>Content types: <c>type_{codename}</c></description></item>
/// <item><description>Assets: <c>asset_{guid}</c></description></item>
/// <item><description>Taxonomies: <c>taxonomy_{group_codename}</c></description></item>
/// </list>
/// These formats align with the cache invalidation strategy in <see cref="IDeliveryCacheManager"/>.
/// </para>
/// </remarks>
internal sealed class DependencyTrackingContext
{
    private readonly HashSet<string> _dependencies = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    /// <summary>
    /// Gets a snapshot of the collected dependency keys.
    /// </summary>
    /// <remarks>
    /// Returns a point-in-time snapshot of the current dependencies.
    /// The snapshot is safe to enumerate without holding any locks.
    /// </remarks>
    public IEnumerable<string> Dependencies
    {
        get
        {
            lock (_lock)
            {
                return [.. _dependencies];
            }
        }
    }

    /// <summary>
    /// Tracks a dependency on a content item by its codename.
    /// </summary>
    /// <param name="codename">
    /// The codename of the content item. If <c>null</c> or empty, the call is ignored.
    /// </param>
    /// <remarks>
    /// Call this method for:
    /// <list type="bullet">
    /// <item><description>The primary content item(s) in the response</description></item>
    /// <item><description>Linked content items (modular content)</description></item>
    /// <item><description>Rich text inline content items</description></item>
    /// </list>
    /// Codenames are case-insensitive and duplicate calls with the same codename are ignored.
    /// </remarks>
    public void TrackItem(string? codename)
    {
        if (string.IsNullOrWhiteSpace(codename))
        {
            return;
        }


        if (IsComponentCodename(codename))
        {
            return;
        }

        var dependencyKey = CacheDependencyKeyBuilder.BuildItemDependencyKey(codename);
        if (dependencyKey is null)
        {
            return;
        }

        lock (_lock)
        {
            _dependencies.Add(dependencyKey);
        }
    }

    private static bool IsComponentCodename(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Component keys are identifiable by the third group (index 2) starting with "01".
        var parts = value.Split('_');
        if (parts.Length <= 2)
        {
            return false;
        }

        var thirdGroup = parts[2];
        return thirdGroup.Length == 4 && thirdGroup.StartsWith("01", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tracks a dependency on a content type by its codename.
    /// </summary>
    /// <param name="typeCodename">
    /// The codename of the content type. If <c>null</c> or empty, the call is ignored.
    /// </param>
    /// <remarks>
    /// Call this method for the content type of every item that contributes to the response
    /// (primary items plus every item in the modular-content graph). Invalidating the resulting
    /// <c>type_{codename}</c> dependency key evicts any cached entry whose payload references
    /// at least one item of that type — the key signal for content-type webhooks.
    /// </remarks>
    public void TrackItemType(string? typeCodename)
    {
        var dependencyKey = CacheDependencyKeyBuilder.BuildTypeDependencyKey(typeCodename);
        if (dependencyKey is null)
        {
            return;
        }

        lock (_lock)
        {
            _dependencies.Add(dependencyKey);
        }
    }

    /// <summary>
    /// Tracks a dependency on an asset by its ID.
    /// </summary>
    /// <param name="assetId">The unique identifier of the asset.</param>
    /// <remarks>
    /// <para>
    /// Call this method for assets referenced in:
    /// <list type="bullet">
    /// <item><description>Rich text image elements</description></item>
    /// <item><description>Asset element values</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Asset IDs are globally unique, so Guid.Empty is a valid value and will be tracked.
    /// Duplicate calls with the same asset ID are ignored.
    /// </para>
    /// </remarks>
    public void TrackAsset(Guid assetId)
    {
        var dependencyKey = CacheDependencyKeyBuilder.BuildAssetDependencyKey(assetId);

        lock (_lock)
        {
            _dependencies.Add(dependencyKey);
        }
    }

    /// <summary>
    /// Tracks a dependency on a taxonomy group by its codename.
    /// </summary>
    /// <param name="taxonomyGroup">
    /// The codename of the taxonomy group. If <c>null</c> or empty, the call is ignored.
    /// </param>
    /// <remarks>
    /// <para>
    /// Call this method when processing taxonomy elements to track dependencies on the
    /// taxonomy group structure itself, not individual terms.
    /// </para>
    /// <para>
    /// Taxonomy group codenames are case-insensitive and duplicate calls are ignored.
    /// </para>
    /// <para>
    /// Note: This tracks the taxonomy group definition. Changes to individual term names
    /// or the term hierarchy will invalidate caches depending on this group.
    /// </para>
    /// </remarks>
    public void TrackTaxonomy(string? taxonomyGroup)
    {
        var dependencyKey = CacheDependencyKeyBuilder.BuildTaxonomyDependencyKey(taxonomyGroup);
        if (dependencyKey is null)
        {
            return;
        }

        lock (_lock)
        {
            _dependencies.Add(dependencyKey);
        }
    }
}
