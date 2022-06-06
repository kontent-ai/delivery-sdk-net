using System;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Builders.DeliveryClient;
using Kentico.Kontent.Delivery.Builders.DeliveryClientFactory;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection
{
    internal class DeliveryClientFactoryBuilder : IDeliveryClientFactoryBuilder
    {
        // TODO 312 OK to instantiate this way?
        // private readonly ConcurrentDictionary<string, IDeliveryClient> _clients = new ConcurrentDictionary<string, IDeliveryClient>();
        private readonly NamedDeliveryClientFactory _clientFactory  = new NamedDeliveryClientFactory();

        public IDeliveryClientFactoryBuilder AddDeliveryClient(string name, Func<IDeliveryClientBuilder, IDeliveryClient> builder)
        {
            _clientFactory.Set(name, builder);
            return this;

        }

        public IDeliveryClientFactory Build()
        {
            return _clientFactory;
        }
    }
}
