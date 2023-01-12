using System;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Caching
{
    /// <summary>
    /// Represents configuration of the <see cref="IDeliveryCacheManager"/>
    /// </summary>
    public class DeliveryCacheOptions
    {
        /// <summary>
        /// Gets or sets the default expiration time
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Gets or sets the default expiration type.
        /// </summary>
        public CacheExpirationType DefaultExpirationType { get; set; } = CacheExpirationType.Sliding;

        /// <summary>
        /// Gets or sets expiration time when the response is stale.
        /// </summary>
        public TimeSpan StaleContentExpiration { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Determines whether to use <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache" /> or <inheritdoc cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>
        /// </summary>
        public CacheTypeEnum CacheType { get; set; } = CacheTypeEnum.Memory;

        /// <summary>
        /// Determines which resilient policy should be used when <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache" /> is not available.
        /// </summary>
        public DistributedCacheResilientPolicy DistributedCacheResilientPolicy { get; set; } = DistributedCacheResilientPolicy.Crash;

        /// <summary>
        /// The name of the service configuration this options object is related to.
        /// </summary>
        [Obsolete("#312")]
        internal string Name { get; set; }
    }
}
