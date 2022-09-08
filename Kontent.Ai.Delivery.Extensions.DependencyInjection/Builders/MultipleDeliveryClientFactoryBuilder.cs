using System;
using System.Collections.Concurrent;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Builders;
using Kontent.Ai.Delivery.Builders.DeliveryClient;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.Configuration;

namespace Kontent.Ai.Delivery.Extensions.DependencyInjection.Builders
{
    internal class MultipleDeliveryClientFactoryBuilder : IMultipleDeliveryClientFactoryBuilder
    {
        private readonly ConcurrentDictionary<string, IDeliveryClient> _clients = new();

        public IMultipleDeliveryClientFactoryBuilder AddDeliveryClient
        (
            string name,
            Func<IDeliveryOptionsBuilder, DeliveryOptions> deliveryOptionsBuilder,
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

        public IMultipleDeliveryClientFactoryBuilder AddDeliveryClientCache
        (
            string name,
            Func<IDeliveryOptionsBuilder, DeliveryOptions> deliveryOptionsBuilder,
            IDeliveryCacheManager cacheManager,
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
            return new MultipleDeliveryClientFactory(_clients);
        }

        private IDeliveryClient BuildDeliveryClient
        (
            Func<IDeliveryOptionsBuilder, DeliveryOptions> deliveryOptionsBuilder,
            Func<IOptionalClientSetup, IOptionalClientSetup> optionalClientSetup
        )
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