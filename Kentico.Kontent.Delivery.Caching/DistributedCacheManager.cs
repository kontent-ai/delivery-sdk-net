using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Caching.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Kentico.Kontent.Delivery.Caching
{
    /// <summary>
    /// Cache responses against the Kentico Kontent Delivery API.
    /// </summary>
    public sealed class DistributedCacheManager : IDeliveryCacheManager
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
            _distributedCache = distributedCache;
            _cacheOptions = cacheOptions.Value ?? new DeliveryCacheOptions();
        }

        /// <summary>
        /// Returns or Adds data to the cache
        /// </summary>
        /// <typeparam name="T">A generic type</typeparam>
        /// <param name="key">A cache key</param>
        /// <param name="valueFactory">A factory which returns a data</param>
        /// <param name="shouldCache"></param>
        /// <param name="dependenciesFactory"></param>
        /// <returns>The data of generic type</returns>
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
                valueCacheOptions.SetSlidingExpiration(_cacheOptions.DefaultExpiration);
            }

            _distributedCache.Set(key, value.ToBson(), valueCacheOptions);

            return value;
        }

        /// <summary>
        /// Attemptes to retrieve data from cache.
        /// </summary>
        /// <typeparam name="T">Type of the response used for deserialization</typeparam>
        /// <param name="key">A cache key</param>
        /// <returns>Returns true along with the deserialized value if the retrieval attempt was successful. Otherwise, returns false and null for the value.</returns>
        public async Task<(bool Success, T Value)> TryGetAsync<T>(string key) where T : class
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var value = (await _distributedCache.GetAsync(key))?.FromBson<T>();
            return (Success: value != null, Value: value);
        }

        /// <summary>
        /// Invalidates data by the key
        /// </summary>
        /// <param name="key">A cache key</param>
        /// <returns></returns>
        public async Task InvalidateDependencyAsync(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            await _distributedCache.RemoveAsync(key); //todo: second param - cancellationtoken
        }

        /// <summary>
        /// Clears cache
        /// </summary>
        /// <returns></returns>
        public Task ClearAsync()
        {
            throw new NotImplementedException("It's not possible to clear a distributed cache.");
        }
    }
}
