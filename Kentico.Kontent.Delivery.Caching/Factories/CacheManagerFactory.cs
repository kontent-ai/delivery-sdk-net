using Kentico.Kontent.Delivery.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Kentico.Kontent.Delivery.Caching.Factories
{
    /// <summary>
    /// A factory for manually create an <see cref="IDeliveryCacheManager"/> instance.
    /// </summary>
    public static class CacheManagerFactory
    {
        /// <summary>
        /// Creates an <see cref="IDeliveryCacheManager"/> instance with a distributed cache.
        /// </summary>
        /// <param name="distributedCache">A <see cref="IDistributedCache"/> instance.</param>
        /// <param name="options">A <see cref="DeliveryCacheOptions"/></param>
        /// <returns>The <see cref="IDeliveryCacheManager"/> instance with a distribute cache.</returns>
        public static IDeliveryCacheManager Create(IDistributedCache distributedCache,
            IOptions<DeliveryCacheOptions> options)
        {
            return new DistributedCacheManager(distributedCache, options);
        }

        /// <summary>
        /// Creates an <see cref="IDeliveryCacheManager"/> instance with a memory cache.
        /// </summary>
        /// <param name="memoryCache">A <see cref="IMemoryCache"/> instance.</param>
        /// <param name="options">A <see cref="DeliveryCacheOptions"/></param>
        /// <returns>The <see cref="IDeliveryCacheManager"/> instance with a memory cache.</returns>
        public static IDeliveryCacheManager Create(IMemoryCache memoryCache,
           IOptions<DeliveryCacheOptions> options)
        {
            return new MemoryCacheManager(memoryCache, options);
        }
    }
}
