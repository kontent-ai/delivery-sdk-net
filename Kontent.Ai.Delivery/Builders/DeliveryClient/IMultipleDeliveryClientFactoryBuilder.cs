using System;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Builders.DeliveryClient;
using Kontent.Ai.Delivery.Configuration;

namespace Kontent.Ai.Delivery.Builders
{
    // TODO 312 - XML comments
    // TODO 312 decide whether this lib is the best for this interface (currently here because of IDeliveryClientBuilder dependency)
    public interface IMultipleDeliveryClientFactoryBuilder
    {
        public IMultipleDeliveryClientFactoryBuilder AddDeliveryClient
        (
            string name,
            Func<IDeliveryOptionsBuilder,
            DeliveryOptions> deliveryOptionsBuilder,
            Func<IOptionalClientSetup, IOptionalClientSetup> optionalClientSetup = null
        );

        public IMultipleDeliveryClientFactoryBuilder AddDeliveryClientCache
        (
            string name, Func<IDeliveryOptionsBuilder,
            DeliveryOptions> deliveryOptionsBuilder,
            IDeliveryCacheManager cacheManager, Func<IOptionalClientSetup,
            IOptionalClientSetup> optionalClientSetup = null
        );

        public IDeliveryClientFactory Build();
    }
}