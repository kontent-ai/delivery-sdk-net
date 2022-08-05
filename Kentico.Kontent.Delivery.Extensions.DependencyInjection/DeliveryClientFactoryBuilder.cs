using System;
using System.Collections.Concurrent;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Builders.DeliveryClient;
using Kentico.Kontent.Delivery.Caching;
using Kentico.Kontent.Delivery.Configuration;
using Kentico.Kontent.Delivery.Extensions.DependencyInjection.Builders;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection
{
    internal class DeliveryClientFactoryBuilder : IDeliveryClientFactoryBuilder
    {
        // TODO 312 OK to instantiate this way?
        private readonly ConcurrentDictionary<string, IDeliveryClient> _clients = new();

        public IDeliveryClientFactoryBuilder AddDeliveryClient
        (
            string name,
            Func<IDeliveryOptionsBuilder,
            DeliveryOptions> deliveryOptionsBuilder,
            Func<IOptionalClientSetup, IOptionalClientSetup> optionalClientSetup = null
        )
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (deliveryOptionsBuilder == null)
            {
                throw new ArgumentNullException(nameof(deliveryOptionsBuilder));
            }

            var client = BuildDeliveryClient(deliveryOptionsBuilder, optionalClientSetup);
            RegisterClient(name, client);

            return this;
        }

        public IDeliveryClientFactoryBuilder AddDeliveryClientCache
        (
            string name, Func<IDeliveryOptionsBuilder,
            DeliveryOptions> deliveryOptionsBuilder,
            IDeliveryCacheManager cacheManager, Func<IOptionalClientSetup,
            IOptionalClientSetup> optionalClientSetup = null
        )
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (deliveryOptionsBuilder == null)
            {
                throw new ArgumentNullException(nameof(deliveryOptionsBuilder));
            }

            if (cacheManager == null)
            {
                throw new ArgumentNullException(nameof(cacheManager));
            }

            var client = BuildDeliveryClient(deliveryOptionsBuilder, optionalClientSetup);
            var clientCache = new DeliveryClientCache(cacheManager, client);
            RegisterClient(name, clientCache);

            return this;
        }

        public IDeliveryClientFactory Build()
        {
            return new DeliveryClientFactory(_clients);
        }

        private IDeliveryClient BuildDeliveryClient(Func<IDeliveryOptionsBuilder, DeliveryOptions> deliveryOptionsBuilder, Func<IOptionalClientSetup, IOptionalClientSetup> optionalClientSetup)
        {
            var setup = DeliveryClientBuilder.WithOptions(deliveryOptionsBuilder);
            return optionalClientSetup != null ? optionalClientSetup(setup).Build() : setup.Build();
        }

        private void RegisterClient(string name, IDeliveryClient client)
        {
            _clients.AddOrUpdate(name, client, (key, oldValue) => client);
        }
    }
}
