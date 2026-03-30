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
    : IDeliveryCacheManager, IDeliveryCachePurger, IFailSafeStateProvider, IDisposable
{
    private readonly FusionCacheManager _inner = FusionCacheManager.CreateMemory(
        memoryCache,
        cacheOptions,
        logger);

    /// <inheritdoc />
    public CacheStorageMode StorageMode => _inner.StorageMode;

    /// <inheritdoc />
    public Task<CacheResult<T>?> GetOrSetAsync<T>(
        string cacheKey,
        Func<CancellationToken, Task<CacheEntry<T>?>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class
        => _inner.GetOrSetAsync(cacheKey, factory, expiration, cancellationToken);

    /// <inheritdoc />
    public Task<bool> InvalidateAsync(string[] dependencyKeys, CancellationToken cancellationToken = default)
        => _inner.InvalidateAsync(dependencyKeys, cancellationToken);

    /// <inheritdoc />
    public Task PurgeAsync(bool allowFailSafe = false, CancellationToken cancellationToken = default)
        => _inner.PurgeAsync(allowFailSafe, cancellationToken);

    bool IFailSafeStateProvider.IsFailSafeActive(string cacheKey)
        => ((IFailSafeStateProvider)_inner).IsFailSafeActive(cacheKey);

    /// <inheritdoc />
    public void Dispose() => _inner.Dispose();
}
