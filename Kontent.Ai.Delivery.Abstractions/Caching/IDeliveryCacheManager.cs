namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Manages caching of Delivery API responses with automatic dependency tracking and invalidation.
/// Implementations should provide thread-safe operations suitable for concurrent access.
/// </summary>
/// <remarks>
/// <para>
/// This interface uses a factory-based <see cref="GetOrSetAsync{T}"/> pattern that atomically
/// checks the cache and populates it on miss. This design enables FusionCache-native stampede
/// protection, eager refresh, and fail-safe — all impossible with a split Get+Set approach.
/// </para>
/// <para>
/// The dependency tracking system enables automatic cache invalidation when content changes.
/// The factory returns a <see cref="CacheEntry{T}"/> that bundles the value with its dependency keys.
/// Later, calling <see cref="InvalidateAsync"/> with any of those dependency keys will invalidate
/// all cache entries that reference them.
/// </para>
/// <para>
/// Dependency key format conventions:
/// <list type="bullet">
/// <item><description>Content items: <c>item_{codename}</c> (e.g., "item_hero")</description></item>
/// <item><description>Assets: <c>asset_{guid}</c> (e.g., "asset_a5e1c4b2-...")</description></item>
/// <item><description>Content types: <c>type_{codename}</c> (e.g., "type_article")</description></item>
/// <item><description>Taxonomies: <c>taxonomy_{group}</c> (e.g., "taxonomy_categories")</description></item>
/// <item><description>Item-list scope: <see cref="DeliveryCacheDependencies.ItemsListScope"/> (synthetic dependency for broad item-list invalidation)</description></item>
/// <item><description>Type-list scope: <see cref="DeliveryCacheDependencies.TypesListScope"/> (synthetic dependency for broad type-list invalidation)</description></item>
/// <item><description>Taxonomy-list scope: <see cref="DeliveryCacheDependencies.TaxonomiesListScope"/> (synthetic dependency for broad taxonomy-list invalidation)</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IDeliveryCacheManager
{
    /// <summary>
    /// Gets the storage mode used by this cache manager.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="CacheStorageMode.HydratedObject"/> (default) stores fully hydrated C# objects,
    /// suitable for in-memory caches. <see cref="CacheStorageMode.RawJson"/> stores raw JSON
    /// strings, suitable for distributed caches that require serialization.
    /// </para>
    /// </remarks>
    CacheStorageMode StorageMode => CacheStorageMode.HydratedObject;

    /// <summary>
    /// Atomically retrieves a cached value or executes the factory to populate the cache.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="cacheKey">The unique key identifying the cache entry.</param>
    /// <param name="factory">
    /// A factory function invoked on cache miss. Returns a <see cref="CacheEntry{T}"/>
    /// containing the value and its dependency tags, or <c>null</c> to signal "don't cache"
    /// (e.g., when an API call fails).
    /// </param>
    /// <param name="expiration">
    /// Optional absolute expiration timespan. If <c>null</c>, the implementation's default expiration is used.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// The cached or freshly computed value, or <c>null</c> if the factory returned <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Implementations must ensure that concurrent calls for the same <paramref name="cacheKey"/>
    /// result in at most one factory invocation (stampede protection).
    /// </para>
    /// <para>
    /// When the factory returns <c>null</c>, the result should not be cached and
    /// <c>null</c> should be returned to the caller.
    /// </para>
    /// </remarks>
    Task<T?> GetOrSetAsync<T>(
        string cacheKey,
        Func<CancellationToken, Task<CacheEntry<T>?>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Invalidates all cache entries that depend on the specified dependency keys.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <param name="dependencyKeys">
    /// One or more dependency keys to invalidate. All cache entries referencing any of these keys
    /// will be removed from the cache.
    /// </param>
    /// <returns>A task representing the asynchronous invalidation operation.</returns>
    /// <remarks>
    /// <para>
    /// This method performs cascade invalidation: if a cache entry depends on any of the specified keys,
    /// it will be removed, regardless of what other dependencies it may have.
    /// </para>
    /// <para>
    /// Invalidating a non-existent dependency key should succeed without error (idempotent operation).
    /// </para>
    /// <para>
    /// Example: If a cached items list depends on "item_hero" and "item_author", calling
    /// <c>InvalidateAsync(dependencyKeys: "item_hero")</c> will invalidate the entire list,
    /// even though "item_author" was not invalidated.
    /// </para>
    /// </remarks>
    Task InvalidateAsync(CancellationToken cancellationToken = default, params string[] dependencyKeys);
}
