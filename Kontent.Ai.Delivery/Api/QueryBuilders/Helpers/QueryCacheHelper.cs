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
    /// Per-manager in-flight requests for a specific (payload,result) operation pair.
    /// Generic static storage ensures independent registries per operation type pair,
    /// preventing type-cast collisions while keeping coalescing scoped to cache manager instances.
    /// </summary>
    private static class InFlightRegistry<TCachePayload, TResult>
        where TCachePayload : class
        where TResult : class
    {
        public static readonly ConditionalWeakTable<IDeliveryCacheManager, InFlightDictionary<TCachePayload, TResult>> Managers = [];

        public static InFlightDictionary<TCachePayload, TResult> GetForManager(IDeliveryCacheManager cacheManager) =>
            Managers.GetOrCreateValue(cacheManager);
    }

    private sealed class InFlightDictionary<TCachePayload, TResult>
        where TCachePayload : class
        where TResult : class
    {
        public ConcurrentDictionary<InFlightKey<TCachePayload>, InFlightRequest<TResult>> Requests { get; } = new();
    }

    private sealed class InFlightRequest<TResult> where TResult : class
    {
        public TaskCompletionSource<CacheFetchResult<TResult>> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private readonly record struct InFlightKey<TCachePayload>(string CacheKey) where TCachePayload : class
    {
        public override int GetHashCode() =>
            HashCode.Combine(typeof(TCachePayload), StringComparer.Ordinal.GetHashCode(CacheKey));
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
    /// <param name="expiration">Optional absolute cache expiration for this entry.</param>
    /// <param name="logger">Optional logger for error logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task TrySetCachedAsync<T>(
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        T value,
        IEnumerable<string> dependencies,
        TimeSpan? expiration,
        ILogger? logger,
        CancellationToken cancellationToken) where T : class
    {
        try
        {
            await cacheManager.SetAsync(cacheKey, value, dependencies,
                expiration, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (logger is not null)
                LoggerMessages.CacheSetFailed(logger, cacheKey, ex);
        }
    }

    /// <summary>
    /// Gets a value from cache or fetches it, with cache stampede protection.
    /// Uses task-based request coalescing to ensure only one caller executes the fetch pipeline
    /// per cache key when multiple concurrent requests miss.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="cacheManager">The cache manager to query.</param>
    /// <param name="cacheKey">The cache key to look up.</param>
    /// <param name="fetchFactory">Factory to fetch the value and its dependencies if not cached.</param>
    /// <param name="expiration">Optional absolute cache expiration for this entry.</param>
    /// <param name="logger">Optional logger for cache hit/miss/error logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the value, dependencies, and whether it was a cache hit.</returns>
    public static async Task<CacheFetchResult<T>> GetOrFetchAsync<T>(
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        Func<CancellationToken, Task<(T? Value, IEnumerable<string> Dependencies)>> fetchFactory,
        TimeSpan? expiration,
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
            expiration,
            logger,
            cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Gets a value from cache with rehydration support for distributed (raw payload) caching.
    /// Uses task-based request coalescing with the same cache-check/fetch behavior as
    /// <see cref="GetOrFetchAsync{T}"/>.
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
    /// <param name="expiration">Optional absolute cache expiration for this entry.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the value, dependencies, and whether it was a cache hit.</returns>
    public static async Task<CacheFetchResult<TResult>> GetOrFetchWithRehydrationAsync<TCachePayload, TResult>(
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        Func<CancellationToken, Task<(TCachePayload? Payload, TResult? ProcessedResult, IEnumerable<string> Dependencies)>> fetchFactory,
        Func<TCachePayload, CancellationToken, Task<TResult>> rehydrateFactory,
        TimeSpan? expiration,
        ILogger? logger,
        CancellationToken cancellationToken)
        where TCachePayload : class
        where TResult : class
        => await GetOrFetchInternal(
            cacheManager,
            cacheKey,
            (cached, key, ct) => TryMaterializeCachedAsync(cached, key, rehydrateFactory, logger, ct),
            fetchFactory,
            expiration,
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
        TimeSpan? expiration,
        ILogger? logger,
        CancellationToken cancellationToken)
        where TCachePayload : class
        where TResult : class
    {
        var requests = InFlightRegistry<TCachePayload, TResult>
            .GetForManager(cacheManager)
            .Requests;
        var inFlightKey = new InFlightKey<TCachePayload>(cacheKey);

        while (true)
        {
            // 1. Fast path: try cache and materialize.
            var cachedResult = await TryGetAndMaterializeCachedAsync(
                cacheManager,
                cacheKey,
                materializeCachedAsync,
                logger,
                cancellationToken).ConfigureAwait(false);

            if (cachedResult is not null)
            {
                return new CacheFetchResult<TResult>(cachedResult, [], IsCacheHit: true);
            }

            var candidateRequest = new InFlightRequest<TResult>();

            // 2. Become the owner if no in-flight request exists for this key.
            if (requests.TryAdd(inFlightKey, candidateRequest))
            {
                _ = ExecuteOwnerFetchAsync(
                    candidateRequest,
                    requests,
                    inFlightKey,
                    cacheManager,
                    cacheKey,
                    materializeCachedAsync,
                    fetchFactory,
                    expiration,
                    logger);

                return await candidateRequest.Completion.Task
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            // 3. Wait for current owner to complete, then loop and re-check cache.
            if (requests.TryGetValue(inFlightKey, out var inFlightRequest))
            {
                await inFlightRequest.Completion.Task
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private static async Task ExecuteOwnerFetchAsync<TCachePayload, TResult>(
        InFlightRequest<TResult> inFlightRequest,
        ConcurrentDictionary<InFlightKey<TCachePayload>, InFlightRequest<TResult>> requests,
        InFlightKey<TCachePayload> inFlightKey,
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        Func<TCachePayload, string, CancellationToken, Task<TResult?>> materializeCachedAsync,
        Func<CancellationToken, Task<(TCachePayload? CachePayload, TResult? Result, IEnumerable<string> Dependencies)>> fetchFactory,
        TimeSpan? expiration,
        ILogger? logger)
        where TCachePayload : class
        where TResult : class
    {
        try
        {
            var result = await ExecuteOwnerFetchCoreAsync(
                cacheManager,
                cacheKey,
                materializeCachedAsync,
                fetchFactory,
                expiration,
                logger).ConfigureAwait(false);

            inFlightRequest.Completion.TrySetResult(result);
        }
        catch (Exception ex)
        {
            inFlightRequest.Completion.TrySetException(ex);
        }
        finally
        {
            requests.TryRemove(new KeyValuePair<InFlightKey<TCachePayload>, InFlightRequest<TResult>>(inFlightKey, inFlightRequest));
        }
    }

    private static async Task<CacheFetchResult<TResult>> ExecuteOwnerFetchCoreAsync<TCachePayload, TResult>(
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        Func<TCachePayload, string, CancellationToken, Task<TResult?>> materializeCachedAsync,
        Func<CancellationToken, Task<(TCachePayload? CachePayload, TResult? Result, IEnumerable<string> Dependencies)>> fetchFactory,
        TimeSpan? expiration,
        ILogger? logger)
        where TCachePayload : class
        where TResult : class
    {
        // Detached fetch semantics: owner execution continues independently of caller cancellation.
        var detachedToken = CancellationToken.None;

        // Preserve double-check behavior from lock-based pipeline.
        var cachedResult = await TryGetAndMaterializeCachedAsync(
            cacheManager,
            cacheKey,
            materializeCachedAsync,
            logger,
            detachedToken).ConfigureAwait(false);

        if (cachedResult is not null)
        {
            return new CacheFetchResult<TResult>(cachedResult, [], IsCacheHit: true);
        }

        var (cachePayload, result, dependencies) = await fetchFactory(detachedToken).ConfigureAwait(false);
        var materializedDependencies = MaterializeDependencies(dependencies);

        if (cachePayload is null)
        {
            if (logger is not null)
            {
                LoggerMessages.QueryCacheStoreSkipped(logger, cacheKey);
            }

            return new CacheFetchResult<TResult>(result, materializedDependencies, IsCacheHit: false);
        }

        if (logger is not null)
        {
            LoggerMessages.QueryCacheStore(
                logger,
                cacheKey,
                FormatExpiration(expiration),
                materializedDependencies.Length);
        }

        await TrySetCachedAsync(cacheManager, cacheKey, cachePayload, materializedDependencies, expiration, logger, detachedToken)
            .ConfigureAwait(false);

        return new CacheFetchResult<TResult>(result, materializedDependencies, IsCacheHit: false);
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

    private static async Task<TResult?> TryGetAndMaterializeCachedAsync<TCachePayload, TResult>(
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        Func<TCachePayload, string, CancellationToken, Task<TResult?>> materializeCachedAsync,
        ILogger? logger,
        CancellationToken cancellationToken)
        where TCachePayload : class
        where TResult : class
    {
        var cached = await TryGetCachedAsync<TCachePayload>(cacheManager, cacheKey, logger, cancellationToken)
            .ConfigureAwait(false);

        return cached is null
            ? null
            : await materializeCachedAsync(cached, cacheKey, cancellationToken).ConfigureAwait(false);
    }

    private static string[] MaterializeDependencies(IEnumerable<string>? dependencies) =>
        dependencies switch
        {
            null => [],
            string[] array => array,
            _ => [.. dependencies]
        };

    private static string FormatExpiration(TimeSpan? expiration) =>
        expiration is { } ttl
            ? ttl.ToString("c", System.Globalization.CultureInfo.InvariantCulture)
            : "manager-default";
}
