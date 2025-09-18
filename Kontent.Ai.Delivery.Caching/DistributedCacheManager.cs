using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Caching.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// Cache responses against the Kontent.ai Delivery API.
/// </summary>
internal class DistributedCacheManager : IDeliveryCacheManager
{
    private readonly IDistributedCache _distributedCache;
    private readonly DeliveryCacheOptions _cacheOptions;
    private readonly ILogger _logger;
    private const string DependencyIndexPrefix = "depidx|";

    /// <summary>
    /// Initializes a new instance of <see cref="DistributedCacheManager"/>
    /// </summary>
    /// <param name="distributedCache">An instance of an object that represent distributed cache</param>
    /// <param name="cacheOptions">The settings of the cache</param>
    /// <param name="loggerFactory">The factory used to create loggers.</param>
    public DistributedCacheManager(IDistributedCache distributedCache,
        IOptions<DeliveryCacheOptions> cacheOptions,
        ILoggerFactory? loggerFactory = null)
    {
        var loggerFactoryToUse = loggerFactory ?? NullLoggerFactory.Instance;

        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _cacheOptions = cacheOptions.Value ?? new DeliveryCacheOptions();
        _logger = loggerFactoryToUse.CreateLogger(nameof(DistributedCacheManager));
    }

    /// <inheritdoc />
    public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> valueFactory, Func<T, bool>? shouldCache = null, Func<T, IEnumerable<string>>? dependenciesFactory = null) where T : class
    {
        try
        {
            var (Success, Value) = await TryGetAsync<T>(key);
            if (Success)
            {
                return Value;
            }
        }
        catch (Exception ex) when (ex is not ArgumentNullException && _cacheOptions.DistributedCacheResilientPolicy == DistributedCacheResilientPolicy.FallbackToApi)
        {
            _logger.LogWarning(ex, "Distributed cache is not available. Default DeliveryClient was used to get content from Delivery API.");
            return await valueFactory();
        }

        var value = await valueFactory();

        // Decide if the value should be cached based on the response
        if (shouldCache != null && !shouldCache(value))
        {
            return value;
        }

        // Set different timeout for stale content
        var valueCacheOptions = new DistributedCacheEntryOptions();
        if (value is IDeliveryResult<object> dr && dr.HasStaleContent)
        {
            valueCacheOptions.SetAbsoluteExpiration(_cacheOptions.StaleContentExpiration);
        }
        else
        {
            switch (_cacheOptions.DefaultExpirationType)
            {
                case CacheExpirationType.Absolute:
                    valueCacheOptions.SetAbsoluteExpiration(_cacheOptions.DefaultExpiration);
                    break;

                case CacheExpirationType.Sliding:
                    valueCacheOptions.SetSlidingExpiration(_cacheOptions.DefaultExpiration);
                    break;
            }
        }

        // Store cached value
        var valueBytes = value.ToMessagePack();
        if (valueBytes == null)
        {
            return value;
        }

        await _distributedCache.SetAsync(key, valueBytes, valueCacheOptions);

        // Store dependency index entries for distributed invalidation
        var dependencies = dependenciesFactory?.Invoke(value);
        if (dependencies != null)
        {
            foreach (var dependency in dependencies)
            {
                if (string.IsNullOrEmpty(dependency))
                {
                    continue;
                }

                var indexKey = GetDependencyIndexKey(dependency);
                var existingIndexBytes = await _distributedCache.GetAsync(indexKey);
                HashSet<string> indexSet = existingIndexBytes.FromMessagePack<HashSet<string>>() ?? new HashSet<string>(StringComparer.Ordinal);
                indexSet.Add(key);

                var indexOptions = new DistributedCacheEntryOptions();
                switch (_cacheOptions.DefaultExpirationType)
                {
                    case CacheExpirationType.Absolute:
                        indexOptions.SetAbsoluteExpiration(_cacheOptions.DefaultExpiration);
                        break;
                    case CacheExpirationType.Sliding:
                        indexOptions.SetSlidingExpiration(_cacheOptions.DefaultExpiration);
                        break;
                }
                var indexBytes = indexSet.ToMessagePack();
                if (indexBytes != null)
                {
                    await _distributedCache.SetAsync(indexKey, indexBytes, indexOptions);
                }
            }
        }

        return value;
    }

    /// <inheritdoc />
    public async Task<(bool Success, T Value)> TryGetAsync<T>(string key) where T : class
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var value = (await _distributedCache.GetAsync(key)).FromMessagePack<T>();
        return (Success: value != null, Value: value!);
    }

    /// <inheritdoc />
    public async Task InvalidateDependencyAsync(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        // First, try to treat the key as a dependency key and invalidate all associated cache entries
        var indexKey = GetDependencyIndexKey(key);
        var indexBytes = await _distributedCache.GetAsync(indexKey);
        var indexSet = indexBytes.FromMessagePack<HashSet<string>>();

        if (indexSet != null)
        {
            foreach (var cacheKey in indexSet)
            {
                await _distributedCache.RemoveAsync(cacheKey);
            }

            // Remove the dependency index itself
            await _distributedCache.RemoveAsync(indexKey);
            return;
        }

        // If there was no dependency index, attempt to remove the key directly (treat as a cache key)
        await _distributedCache.RemoveAsync(key);
    }

    /// <inheritdoc />
    public Task ClearAsync()
    {
        throw new NotImplementedException("It's not possible to clear a distributed cache.");
    }

    private static string GetDependencyIndexKey(string dependencyKey) => string.Concat(DependencyIndexPrefix, dependencyKey);
}
