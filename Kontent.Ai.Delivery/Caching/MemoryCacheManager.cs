using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// In-memory implementation of <see cref="IDeliveryCacheManager"/> backed by FusionCache.
/// </summary>
internal sealed class MemoryCacheManager(
    IMemoryCache memoryCache,
    DeliveryCacheOptions cacheOptions,
    ILogger<MemoryCacheManager>? logger = null)
    : IDeliveryCacheManager, IDeliveryCachePurger, IDisposable
{
    private readonly FusionCacheManager _inner = FusionCacheManager.CreateMemory(
        memoryCache,
        cacheOptions,
        logger);

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
