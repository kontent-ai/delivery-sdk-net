using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;

/// <summary>
/// Provides centralized cache operations with consistent logging for query builders.
/// </summary>
internal static class QueryCacheHelper
{
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
}
