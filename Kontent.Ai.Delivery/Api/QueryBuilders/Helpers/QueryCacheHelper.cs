using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
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
    /// Per-manager lock dictionaries to prevent cache stampede (thundering herd) on concurrent cache misses.
    /// ConditionalWeakTable ensures locks are scoped per cache manager instance (avoiding cross-client collisions)
    /// and automatically cleaned up when the cache manager is garbage collected.
    /// </summary>
    private static readonly ConditionalWeakTable<IDeliveryCacheManager, LockDictionary> _managerLocks = [];

    /// <summary>
    /// Wrapper around ConcurrentDictionary with cleanup tracking.
    /// </summary>
    private sealed class LockDictionary
    {
        public ConcurrentDictionary<string, KeyLock> Locks { get; } = new();
        private int _cleanupCounter;

        /// <summary>
        /// Increments counter and returns true if cleanup should run.
        /// </summary>
        public bool ShouldCleanup() => Interlocked.Increment(ref _cleanupCounter) % CleanupInterval == 0;
    }

    /// <summary>
    /// Time in milliseconds after which an unused lock is eligible for cleanup.
    /// </summary>
    private const long LockExpirationMs = 5 * 60 * 1000; // 5 minutes

    /// <summary>
    /// Cleanup runs every N lock acquisitions to avoid overhead on every call.
    /// </summary>
    private const int CleanupInterval = 100;

    /// <summary>
    /// Wraps a SemaphoreSlim with usage tracking for safe expiration-based cleanup.
    /// </summary>
    private sealed class KeyLock
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        private long _lastUsedTicks = Environment.TickCount64;
        private int _inFlight;

        /// <summary>
        /// Increments the in-flight counter. Call immediately after GetOrAdd, before WaitAsync.
        /// </summary>
        public void IncrementInFlight() => Interlocked.Increment(ref _inFlight);

        /// <summary>
        /// Decrements the in-flight counter. Call in finally after Semaphore.Release().
        /// </summary>
        public void DecrementInFlight()
        {
            Interlocked.Decrement(ref _inFlight);
            _lastUsedTicks = Environment.TickCount64;
        }

        /// <summary>
        /// Returns true if lock is expired and no threads are using or waiting for it.
        /// </summary>
        public bool CanBeRemoved =>
            Volatile.Read(ref _inFlight) == 0 &&
            Environment.TickCount64 - Volatile.Read(ref _lastUsedTicks) > LockExpirationMs;
    }

    private readonly struct KeyLockScope(KeyLock keyLock) : IDisposable
    {
        private readonly KeyLock _keyLock = keyLock;

        public void Dispose()
        {
            _keyLock.Semaphore.Release();
            _keyLock.DecrementInFlight();
        }
    }

    private static async ValueTask<KeyLockScope> AcquireKeyLockAsync(KeyLock keyLock, CancellationToken cancellationToken, bool alreadyInFlight = false)
    {
        if (!alreadyInFlight)
            keyLock.IncrementInFlight();

        try
        {
            await keyLock.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new KeyLockScope(keyLock);
        }
        catch
        {
            keyLock.DecrementInFlight();
            throw;
        }
    }

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

            if (cached is not null)
            {
                if (logger is not null)
                    LoggerMessages.QueryCacheHit(logger, cacheKey);

                return cached;
            }

            if (logger is not null)
                LoggerMessages.QueryCacheMiss(logger, cacheKey);

            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (logger is not null)
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
            if (logger is not null)
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
        => await GetOrFetchInternal(
            cacheManager,
            cacheKey,
            static (cached, _, _) => Task.FromResult<T?>(cached),
            async ct =>
            {
                var (value, dependencies) = await fetchFactory(ct).ConfigureAwait(false);
                return (CachePayload: value, Result: value, Dependencies: dependencies);
            },
            logger,
            cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Gets a value from cache with rehydration support for distributed (raw payload) caching.
    /// Uses check-lock-check pattern with stampede protection, same as <see cref="GetOrFetchAsync{T}"/>.
    /// On cache hit, attempts to rehydrate the raw payload. If rehydration fails, falls through
    /// to fresh fetch and overwrites the stale cache entry.
    /// </summary>
    /// <typeparam name="TCachePayload">The raw cache payload type (e.g., CachedRawItemsPayload).</typeparam>
    /// <typeparam name="TResult">The final result type returned to the caller.</typeparam>
    /// <param name="cacheManager">The cache manager to query.</param>
    /// <param name="cacheKey">The cache key to look up.</param>
    /// <param name="fetchFactory">
    /// Factory to fetch from API. Returns the cache payload, the processed result, and dependency keys.
    /// The payload may be null if the API call fails.
    /// </param>
    /// <param name="rehydrateFactory">Factory to rehydrate a cached payload into the final result.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the value, dependencies, and whether it was a cache hit.</returns>
    public static async Task<CacheFetchResult<TResult>> GetOrFetchWithRehydrationAsync<TCachePayload, TResult>(
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        Func<CancellationToken, Task<(TCachePayload? Payload, TResult? ProcessedResult, IEnumerable<string> Dependencies)>> fetchFactory,
        Func<TCachePayload, CancellationToken, Task<TResult>> rehydrateFactory,
        ILogger? logger,
        CancellationToken cancellationToken)
        where TCachePayload : class
        where TResult : class
        => await GetOrFetchInternal(
            cacheManager,
            cacheKey,
            (cached, key, ct) => TryMaterializeCachedAsync(cached, key, rehydrateFactory, logger, ct),
            fetchFactory,
            logger,
            cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Shared cache retrieval and stampede-protected fetch pipeline.
    /// Materializes cache hits from payload type to result type.
    /// </summary>
    private static async Task<CacheFetchResult<TResult>> GetOrFetchInternal<TCachePayload, TResult>(
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        Func<TCachePayload, string, CancellationToken, Task<TResult?>> materializeCachedAsync,
        Func<CancellationToken, Task<(TCachePayload? CachePayload, TResult? Result, IEnumerable<string> Dependencies)>> fetchFactory,
        ILogger? logger,
        CancellationToken cancellationToken)
        where TCachePayload : class
        where TResult : class
    {
        // 1. Fast path: try cache and materialize.
        var cached = await TryGetCachedAsync<TCachePayload>(cacheManager, cacheKey, logger, cancellationToken)
            .ConfigureAwait(false);
        if (cached is not null)
        {
            var materialized = await materializeCachedAsync(cached, cacheKey, cancellationToken).ConfigureAwait(false);
            if (materialized is not null)
            {
                return new CacheFetchResult<TResult>(materialized, [], IsCacheHit: true);
            }
        }

        // 2. Acquire per-key lock to prevent stampede.
        using var _ = await AcquireManagerKeyLockAsync(cacheManager, cacheKey, cancellationToken).ConfigureAwait(false);

        // 3. Double-check after acquiring lock.
        cached = await TryGetCachedAsync<TCachePayload>(cacheManager, cacheKey, logger, cancellationToken)
            .ConfigureAwait(false);
        if (cached is not null)
        {
            var materialized = await materializeCachedAsync(cached, cacheKey, cancellationToken).ConfigureAwait(false);
            if (materialized is not null)
            {
                return new CacheFetchResult<TResult>(materialized, [], IsCacheHit: true);
            }
        }

        // 4. Fetch fresh.
        var (cachePayload, result, dependencies) = await fetchFactory(cancellationToken).ConfigureAwait(false);

        // 5. Cache payload if available.
        if (cachePayload is not null)
        {
            await TrySetCachedAsync(cacheManager, cacheKey, cachePayload, dependencies, logger, cancellationToken)
                .ConfigureAwait(false);
        }

        return new CacheFetchResult<TResult>(result, dependencies, IsCacheHit: false);
    }

    private static async ValueTask<KeyLockScope> AcquireManagerKeyLockAsync(
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        CancellationToken cancellationToken)
    {
        var lockDict = _managerLocks.GetOrCreateValue(cacheManager);
        var keyLock = lockDict.Locks.GetOrAdd(cacheKey, _ => new KeyLock());
        keyLock.IncrementInFlight(); // Must be immediately after GetOrAdd, before cleanup can run.

        // Periodically clean up expired locks (lazy cleanup to avoid background threads).
        if (lockDict.ShouldCleanup())
        {
            CleanupExpiredLocks(lockDict.Locks);
        }

        return await AcquireKeyLockAsync(keyLock, cancellationToken, alreadyInFlight: true).ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to materialize a cached payload. Returns null if materialization fails.
    /// </summary>
    private static async Task<TResult?> TryMaterializeCachedAsync<TCachePayload, TResult>(
        TCachePayload cached,
        string cacheKey,
        Func<TCachePayload, CancellationToken, Task<TResult>> materializeFactory,
        ILogger? logger,
        CancellationToken cancellationToken)
        where TCachePayload : class
        where TResult : class
    {
        try
        {
            return await materializeFactory(cached, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (logger is not null)
                LoggerMessages.CacheDeserializationFailed(logger, cacheKey, typeof(TCachePayload).Name, ex);
            return null;
        }
    }

    /// <summary>
    /// Removes expired locks that are not in use from the specified dictionary.
    /// Called lazily during lock acquisition to avoid background threads.
    /// </summary>
    /// <remarks>
    /// This is O(n) over all keys for the given cache manager. For typical SDK usage
    /// (bounded content types/items), this is acceptable. For extreme key cardinality,
    /// consider sampling-based cleanup.
    /// </remarks>
    private static void CleanupExpiredLocks(ConcurrentDictionary<string, KeyLock> locks)
    {
        foreach (var kvp in locks)
        {
            if (kvp.Value.CanBeRemoved)
            {
                locks.TryRemove(kvp.Key, out _);
            }
        }
    }
}
