using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// Distributed implementation of <see cref="IDeliveryCacheManager"/> backed by FusionCache.
/// </summary>
internal sealed class DistributedCacheManager(
    IDistributedCache cache,
    DeliveryCacheOptions cacheOptions,
    JsonSerializerOptions? jsonSerializerOptions = null,
    ILogger<DistributedCacheManager>? logger = null)
    : IDeliveryCacheManager, IDeliveryCachePurger, IFailSafeStateProvider, IDisposable
{
    private readonly FusionCacheManager _inner = FusionCacheManager.CreateDistributed(
        cache,
        cacheOptions,
        jsonSerializerOptions,
        logger);

    /// <inheritdoc />
    public CacheStorageMode StorageMode => _inner.StorageMode;

    /// <inheritdoc />
    public Task<T?> GetOrSetAsync<T>(
        string cacheKey,
        Func<CancellationToken, Task<CacheEntry<T>?>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class
        => _inner.GetOrSetAsync(cacheKey, factory, expiration, cancellationToken);

    /// <inheritdoc />
    public Task InvalidateAsync(CancellationToken cancellationToken = default, params string[] dependencyKeys)
        => _inner.InvalidateAsync(cancellationToken, dependencyKeys);

    /// <inheritdoc />
    public Task PurgeAsync(bool allowFailSafe = false, CancellationToken cancellationToken = default)
        => _inner.PurgeAsync(allowFailSafe, cancellationToken);

    bool IFailSafeStateProvider.IsFailSafeActive(string cacheKey)
        => ((IFailSafeStateProvider)_inner).IsFailSafeActive(cacheKey);

    /// <inheritdoc />
    public void Dispose() => _inner.Dispose();
}
