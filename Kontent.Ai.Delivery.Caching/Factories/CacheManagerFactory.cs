﻿using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.Caching.Factories
{
    /// <summary>
    /// A factory class for manually create an <see cref="IDeliveryCacheManager"/> instance.
    /// </summary>
    public static class CacheManagerFactory
    {
        /// <summary>
        /// Creates an <see cref="IDeliveryCacheManager"/> instance with a distributed cache.
        /// </summary>
        /// <param name="distributedCache">A <see cref="IDistributedCache"/> instance.</param>
        /// <param name="options">A <see cref="DeliveryCacheOptions"/></param>
        /// <param name="loggerFactory">The factory used to create loggers.</param>
        /// <returns>The <see cref="IDeliveryCacheManager"/> instance with a distribute cache.</returns>
        public static IDeliveryCacheManager Create(IDistributedCache distributedCache,
            IOptions<DeliveryCacheOptions> options,
            ILoggerFactory loggerFactory = null)
        {
            return new DistributedCacheManager(distributedCache, options, loggerFactory ?? NullLoggerFactory.Instance);
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
