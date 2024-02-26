using System;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Builders.DeliveryClient;
using Kontent.Ai.Delivery.Configuration;

namespace Kontent.Ai.Delivery.Builders
{
    /// <summary>
    /// The builder capable of providing multiple instances of delivery client with different configurations.
    /// Typically used for different environment clients, or for client to the preview and delivery API.
    /// </summary>
    public interface IMultipleDeliveryClientFactoryBuilder
    {
        /// <summary>
        /// Register delivery client instance.
        /// </summary>
        /// <param name="name">Client identifier.</param>
        /// <param name="deliveryOptionsBuilder">Hook for <see cref="DeliveryOptions"/> creation using <see cref="IDeliveryOptionsBuilder"/>.</param>
        /// <param name="optionalClientSetup">Hook for <see cref="IOptionalClientSetup"/> creation using <see cref="IOptionalClientSetup"/>.</param>
        /// <returns>Instance of <see cref="IMultipleDeliveryClientFactoryBuilder"/>.</returns>
        public IMultipleDeliveryClientFactoryBuilder AddDeliveryClient
        (
            string name,
            Func<IDeliveryOptionsBuilder, DeliveryOptions> deliveryOptionsBuilder,
            Func<IOptionalClientSetup, IOptionalClientSetup> optionalClientSetup = null
        );

        /// <summary>
        /// Register delivery client instance with cache.
        /// </summary>
        /// <param name="name">Client identifier.</param>
        /// <param name="deliveryOptionsBuilder">Hook for <see cref="DeliveryOptions"/> creation using <see cref="IDeliveryOptionsBuilder"/>.</param>
        /// <param name="cacheManager">Cache manager setup-</param>
        /// <param name="optionalClientSetup">Hook for <see cref="IOptionalClientSetup"/> creation using <see cref="IOptionalClientSetup"/>.</param>
        /// <returns>Instance of <see cref="IMultipleDeliveryClientFactoryBuilder"/>.</returns>
        public IMultipleDeliveryClientFactoryBuilder AddDeliveryClientCache
        (
            string name,
            Func<IDeliveryOptionsBuilder, DeliveryOptions> deliveryOptionsBuilder,
            IDeliveryCacheManager cacheManager,
            Func<IOptionalClientSetup, IOptionalClientSetup> optionalClientSetup = null
        );

        /// <summary>
        /// Build up the factory for registration.
        /// </summary>
        /// <returns>An instance of <see cref="IDeliveryClientFactory"/></returns>
        public IDeliveryClientFactory Build();
    }
}