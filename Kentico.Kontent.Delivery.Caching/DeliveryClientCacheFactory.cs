using Kentico.Kontent.Delivery.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kentico.Kontent.Delivery.Caching
{
    /// <summary>
    /// A factory class for cache decorator of <see cref="IDeliveryClient"/>
    /// </summary>
    public class DeliveryClientCacheFactory : IDeliveryClientCacheFactory
    {
        /// <summary>
        /// Returns a cache decorator for <see cref="IDeliveryClient"/>.
        /// </summary>
        /// <param name="cacheManager"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public IDeliveryClient Create(IDeliveryCacheManager cacheManager, IDeliveryClient client)
        {
            return new DeliveryClientCache(cacheManager, client);
        }
    }
}
