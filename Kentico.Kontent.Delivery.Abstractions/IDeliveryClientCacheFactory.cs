namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Defines a method for creating a cache decorator a <see cref="IDeliveryClient"/>
    /// </summary>
    public interface IDeliveryClientCacheFactory
    {
        /// <summary>
        /// Returns a new cache decorator for <see cref="IDeliveryClient"/>
        /// </summary>
        /// <param name="cacheManager"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        IDeliveryClient Create(IDeliveryCacheManager cacheManager, IDeliveryClient client);
    }
}
