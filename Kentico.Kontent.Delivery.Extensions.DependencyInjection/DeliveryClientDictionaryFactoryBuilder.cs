using System;
using System.Collections.Concurrent;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Builders.DeliveryClient;
using Kentico.Kontent.Delivery.Builders.DeliveryClientFactory;
using Kentico.Kontent.Delivery.Configuration;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection
{
    internal class DeliveryClientDictionaryFactoryBuilder : IDeliveryClientFactoryBuilder
    {
        // TODO 312 OK to instantiate this way?
        private readonly ConcurrentDictionary<string, IDeliveryClient> _clients = new();

        public IDeliveryClientFactoryBuilder AddDeliveryClient(string name, Func<IDeliveryOptionsBuilder, DeliveryOptions> deliveryOptionsBuilder, Func<IOptionalClientSetup, IOptionalClientSetup> deliveryClientBuilder)
        {
            var setup = DeliveryClientBuilder.WithOptions(deliveryOptionsBuilder);
            var client = deliveryClientBuilder(setup).Build();
            // TODO 312 - warning for rewriting the same client
            _clients.AddOrUpdate(name, client, (key, oldValue) => client);

            return this;
        }

        public IDeliveryClientFactory Build()
        {
            return new DeliveryClientDictionaryFactory(_clients);
        }
    }
}
