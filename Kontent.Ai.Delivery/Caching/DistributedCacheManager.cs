using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// Distributed implementation of <see cref="IDeliveryCacheManager"/> backed by FusionCache.
/// </summary>
internal sealed class DistributedCacheManager(
    IDistributedCache cache,
    string? keyPrefix,
    TimeSpan? defaultExpiration,
    JsonSerializerOptions? jsonSerializerOptions,
    ILogger<DistributedCacheManager>? logger = null)
    : IDeliveryCacheManager, IDeliveryCachePurger, IDisposable
{
    private readonly FusionCacheManager _inner = FusionCacheManager.CreateDistributed(
        cache,
        keyPrefix,
        defaultExpiration,
        jsonSerializerOptions,
        logger);

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedCacheManager"/> class.
    /// </summary>
    /// <param name="cache">The distributed cache implementation (Redis, SQL Server, etc.).</param>
    /// <param name="keyPrefix">
    /// Optional prefix for all cache keys. Used to isolate cache entries when multiple clients share the same distributed cache.
    /// </param>
    /// <param name="defaultExpiration">Default expiration for cache entries. If null, defaults to 1 hour.</param>
    /// <param name="logger">Optional logger for cache operations.</param>
    public DistributedCacheManager(
        IDistributedCache cache,
        string? keyPrefix = null,
        TimeSpan? defaultExpiration = null,
        ILogger<DistributedCacheManager>? logger = null)
        : this(cache, keyPrefix, defaultExpiration, jsonSerializerOptions: null, logger)
    {
    }

    /// <inheritdoc />
    public CacheStorageMode StorageMode => _inner.StorageMode;

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
        where T : class
        => _inner.GetAsync<T>(cacheKey, cancellationToken);

    /// <inheritdoc />
    public Task SetAsync<T>(
        string cacheKey,
        T value,
        IEnumerable<string> dependencies,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class
        => _inner.SetAsync(cacheKey, value, dependencies, expiration, cancellationToken);

    /// <inheritdoc />
    public Task InvalidateAsync(CancellationToken cancellationToken = default, params string[] dependencyKeys)
        => _inner.InvalidateAsync(cancellationToken, dependencyKeys);

    /// <inheritdoc />
    public Task PurgeAsync(bool allowFailSafe = false, CancellationToken cancellationToken = default)
        => _inner.PurgeAsync(allowFailSafe, cancellationToken);

    /// <inheritdoc />
    public void Dispose() => _inner.Dispose();
}
