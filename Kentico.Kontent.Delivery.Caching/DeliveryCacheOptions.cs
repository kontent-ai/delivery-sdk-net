using System;

namespace Kentico.Kontent.Delivery.Caching
{
    /// <summary>
    /// Represents configuration of the <see cref="DeliveryCacheManager"/>
    /// </summary>
    public class DeliveryCacheOptions
    {
        /// <summary>
        /// Gets or sets the default expiration time
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Gets or sets expiration time when the response is stale.
        /// </summary>
        public TimeSpan StaleContentExpiration { get; set; } = TimeSpan.FromSeconds(10);
    }
}
