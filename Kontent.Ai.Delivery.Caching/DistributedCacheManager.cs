using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Caching.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.Caching
{
    /// <summary>
    /// Cache responses against the Kontent Delivery API.
    /// </summary>
    internal class DistributedCacheManager : IDeliveryCacheManager
    {
        private readonly IDistributedCache _distributedCache;
        private readonly DeliveryCacheOptions _cacheOptions;

        /// <summary>
        /// Initializes a new instance of <see cref="DistributedCacheManager"/>
        /// </summary>
        /// <param name="distributedCache">An instance of an object that represent distributed cache</param>
        /// <param name="cacheOptions">The settings of the cache</param>
        public DistributedCacheManager(IDistributedCache distributedCache, IOptions<DeliveryCacheOptions> cacheOptions)
        {
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _cacheOptions = cacheOptions.Value ?? new DeliveryCacheOptions();
        }

        /// <inheritdoc />
        public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> valueFactory, Func<T, bool> shouldCache = null, Func<T, IEnumerable<string>> dependenciesFactory = null) where T : class
        {
            var (Success, Value) = await TryGetAsync<T>(key);
            if (Success)
            {
                return Value;
            }

            var value = await valueFactory();

            // Decide if the value should be cached based on the response
            if (shouldCache != null && !shouldCache(value))
            {
                return value;
            }

            // Set different timeout for stale content
            var valueCacheOptions = new DistributedCacheEntryOptions();
            if (value is IResponse ar && ar.ApiResponse.HasStaleContent)
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

            await _distributedCache.SetAsync(key, value.ToBson(), valueCacheOptions);

            return value;
        }

        /// <inheritdoc />
        public async Task<(bool Success, T Value)> TryGetAsync<T>(string key) where T : class
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var value = (await _distributedCache.GetAsync(key))?.FromBson<T>();
            return (Success: value != null, Value: value);
        }

        /// <inheritdoc />
        public async Task InvalidateDependencyAsync(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            await _distributedCache.RemoveAsync(key);
        }

        /// <inheritdoc />
        public Task ClearAsync()
        {
            throw new NotImplementedException("It's not possible to clear a distributed cache.");
        }
    }
}
