using System;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Builders.DeliveryClient;
using Kentico.Kontent.Delivery.Caching;
using Kentico.Kontent.Delivery.Configuration;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection.Builders
{
    // TODO 312 - XML comments
    // TODO 312 decide whether this lib is the best for this interface (currently here because of IDeliveryClientBuilder dependency)
    public interface IDeliveryClientFactoryBuilder
    {
        public IDeliveryClientFactoryBuilder AddDeliveryClient
        (
            string name,
            Func<IDeliveryOptionsBuilder,
            DeliveryOptions> deliveryOptionsBuilder,
            Func<IOptionalClientSetup, IOptionalClientSetup> optionalClientSetup = null
        );

        public IDeliveryClientFactoryBuilder AddDeliveryClientCache
        (
            string name, Func<IDeliveryOptionsBuilder,
            DeliveryOptions> deliveryOptionsBuilder,
            IDeliveryCacheManager cacheManager, Func<IOptionalClientSetup,
            IOptionalClientSetup> optionalClientSetup = null
        );
        
        public IDeliveryClientFactory Build();
    }
}