using Kentico.Kontent.Delivery.Abstractions;
using System;
using System.Collections.Concurrent;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection
{
    internal class DeliveryClientDictionaryFactory : IDeliveryClientFactory
    {
        private readonly ConcurrentDictionary<string, IDeliveryClient> _clients = new ConcurrentDictionary<string, IDeliveryClient>();

        public DeliveryClientDictionaryFactory(ConcurrentDictionary<string, IDeliveryClient> clients)
        {
            _clients = new(clients);
        }

        /// <inheritdoc />
        public IDeliveryClient Get(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!_clients.TryGetValue(name, out var client))
            {
                // TODO 312 - add some love
                throw new ArgumentException($"The named client '{name}' does not exist.");
            }

            return client;
        }


        // TODO log warning for registering client with the same name
        public IDeliveryClient Get() => throw new NotImplementedException("If you want to use single delivery client use AddDeliveryClient when registering");
    }
}
