namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Manages caching of Delivery API responses with automatic dependency tracking and invalidation.
/// Implementations should provide thread-safe operations suitable for concurrent access.
/// </summary>
/// <remarks>
/// <para>
/// This interface follows modern .NET caching patterns, separating retrieval (<see cref="GetAsync{T}"/>),
/// storage (<see cref="SetAsync{T}"/>), and invalidation (<see cref="InvalidateAsync"/>) concerns.
/// </para>
/// <para>
/// The dependency tracking system enables automatic cache invalidation when content changes.
/// When storing a cache entry, you specify which content items, assets, or taxonomies it depends on.
/// Later, calling <see cref="InvalidateAsync"/> with any of those dependency keys will invalidate
/// all cache entries that reference them.
/// </para>
/// <para>
/// Dependency key format conventions:
/// <list type="bullet">
/// <item><description>Content items: <c>item_{codename}</c> (e.g., "item_hero")</description></item>
/// <item><description>Assets: <c>asset_{guid}</c> (e.g., "asset_a5e1c4b2-...")</description></item>
/// <item><description>Taxonomies: <c>taxonomy_{group}</c> (e.g., "taxonomy_categories")</description></item>
/// <item><description>Item-list scope: <see cref="DeliveryCacheDependencies.ItemsListScope"/> (synthetic dependency for broad item-list invalidation)</description></item>
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
    /// Attempts to retrieve a cached value by its key.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="cacheKey">The unique key identifying the cache entry.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the cached value if found;
    /// otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// This method should return <c>null</c> for cache misses or expired entries, not throw exceptions.
    /// Implementations should handle deserialization errors gracefully by treating them as cache misses.
    /// </remarks>
    Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Stores a value in the cache with associated dependency keys for automatic invalidation.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="cacheKey">The unique key under which to store the value. Must not be <c>null</c> or empty.</param>
    /// <param name="value">The value to cache. Must not be <c>null</c>.</param>
    /// <param name="dependencies">
    /// A collection of dependency keys that, when invalidated, will also invalidate this cache entry.
    /// Use standardized key formats (see <see cref="IDeliveryCacheManager"/> remarks).
    /// Must not be <c>null</c>, but may be empty.
    /// </param>
    /// <param name="expiration">
    /// Optional absolute expiration timespan. If <c>null</c>, the implementation's default expiration is used.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous storage operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cacheKey"/>, <paramref name="value"/>, or <paramref name="dependencies"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="cacheKey"/> is empty or whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Implementations should create a reverse index mapping each dependency key to all cache entries
    /// that reference it, enabling efficient invalidation via <see cref="InvalidateAsync"/>.
    /// </para>
    /// <para>
    /// If the cache write fails, implementations should throw an exception rather than silently fail,
    /// allowing the calling code to handle the error appropriately.
    /// </para>
    /// </remarks>
    Task SetAsync<T>(
        string cacheKey,
        T value,
        IEnumerable<string> dependencies,
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
