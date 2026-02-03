using System.Collections.Concurrent;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;

/// <summary>
/// Result of a cache-protected fetch operation.
/// </summary>
/// <typeparam name="T">The type of the cached value.</typeparam>
internal readonly record struct CacheFetchResult<T>(T? Value, IEnumerable<string> Dependencies, bool IsCacheHit) where T : class;

/// <summary>
/// Provides centralized cache operations with consistent logging for query builders.
/// Includes cache stampede protection via per-key request coalescing.
/// </summary>
internal static class QueryCacheHelper
{
    /// <summary>
    /// Per-key locks to prevent cache stampede (thundering herd) on concurrent cache misses.
    /// </summary>
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();

    /// <summary>
    /// Attempts to retrieve a cached value with logging. Returns null on cache miss or error.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="cacheManager">The cache manager to query.</param>
    /// <param name="cacheKey">The cache key to look up.</param>
    /// <param name="logger">Optional logger for cache hit/miss/error logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached value if found, otherwise null.</returns>
    public static async Task<T?> TryGetCachedAsync<T>(
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        ILogger? logger,
        CancellationToken cancellationToken) where T : class
    {
        try
        {
            var cached = await cacheManager.GetAsync<T>(cacheKey, cancellationToken)
                .ConfigureAwait(false);

            if (cached != null)
            {
                if (logger != null)
                    LoggerMessages.QueryCacheHit(logger, cacheKey);
                return cached;
            }

            if (logger != null)
                LoggerMessages.QueryCacheMiss(logger, cacheKey);

            return null;
        }
        catch (Exception ex)
        {
            if (logger != null)
                LoggerMessages.CacheGetFailed(logger, cacheKey, ex);
            return null;
        }
    }

    /// <summary>
    /// Attempts to store a value in cache with logging. Errors are logged but not thrown.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="cacheManager">The cache manager to use.</param>
    /// <param name="cacheKey">The cache key to store under.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="dependencies">Dependency keys for cache invalidation.</param>
    /// <param name="logger">Optional logger for error logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task TrySetCachedAsync<T>(
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        T value,
        IEnumerable<string> dependencies,
        ILogger? logger,
        CancellationToken cancellationToken) where T : class
    {
        try
        {
            await cacheManager.SetAsync(cacheKey, value, dependencies,
                expiration: null, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (logger != null)
                LoggerMessages.CacheSetFailed(logger, cacheKey, ex);
        }
    }

    /// <summary>
    /// Gets a value from cache or fetches it, with cache stampede protection.
    /// Uses per-key locking with double-check pattern to ensure only one caller
    /// fetches the value when multiple concurrent requests have a cache miss.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="cacheManager">The cache manager to query.</param>
    /// <param name="cacheKey">The cache key to look up.</param>
    /// <param name="fetchFactory">Factory to fetch the value and its dependencies if not cached.</param>
    /// <param name="logger">Optional logger for cache hit/miss/error logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the value, dependencies, and whether it was a cache hit.</returns>
    public static async Task<CacheFetchResult<T>> GetOrFetchAsync<T>(
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        Func<CancellationToken, Task<(T? Value, IEnumerable<string> Dependencies)>> fetchFactory,
        ILogger? logger,
        CancellationToken cancellationToken) where T : class
    {
        // 1. Fast path: cache hit (no lock needed)
        var cached = await TryGetCachedAsync<T>(cacheManager, cacheKey, logger, cancellationToken)
            .ConfigureAwait(false);
        if (cached != null)
            return new CacheFetchResult<T>(cached, [], IsCacheHit: true);

        // 2. Acquire per-key lock to prevent stampede
        var keyLock = _keyLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await keyLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // 3. Double-check after acquiring lock (another thread may have populated cache)
            cached = await TryGetCachedAsync<T>(cacheManager, cacheKey, logger, cancellationToken)
                .ConfigureAwait(false);
            if (cached != null)
                return new CacheFetchResult<T>(cached, [], IsCacheHit: true);

            // 4. Only one caller executes fetch
            var (value, dependencies) = await fetchFactory(cancellationToken).ConfigureAwait(false);

            // 5. Cache the result if not null
            if (value != null)
            {
                await TrySetCachedAsync(cacheManager, cacheKey, value, dependencies, logger, cancellationToken)
                    .ConfigureAwait(false);
            }

            return new CacheFetchResult<T>(value, dependencies, IsCacheHit: false);
        }
        finally
        {
            keyLock.Release();
        }
    }
}
