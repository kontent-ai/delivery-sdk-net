using Kentico.Kontent.Delivery.Abstractions;
using System;
using System.Collections.Concurrent;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection
{
    internal class DeliveryClientFactory : IDeliveryClientFactory
    {
        private readonly ConcurrentDictionary<string, IDeliveryClient> _clients = new ConcurrentDictionary<string, IDeliveryClient>();

        public DeliveryClientFactory(ConcurrentDictionary<string, IDeliveryClient> clients)
        {
            if (clients == null)
            {
                throw new ArgumentNullException(nameof(clients));
            }
            
            _clients = new(clients);
        }

        /// <inheritdoc />
        public IDeliveryClient Get(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if(string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be empty", nameof(name));
            }

            if (!_clients.TryGetValue(name, out var client))
            {
                throw new ArgumentException($"The named client '{name}' does not exist.");
            }

            return client;
        }

        public IDeliveryClient Get() => throw new NotImplementedException("If you want to use single delivery client use AddDeliveryClient when registering");
    }
}
