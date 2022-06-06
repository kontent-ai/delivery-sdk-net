using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Builders.DeliveryClient;
using Kentico.Kontent.Delivery.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection
{
    internal class NamedDeliveryClientFactory : IDeliveryClientFactory
    {
        private readonly IOptionsMonitor<DeliveryOptions> _deliveryOptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly INamedServiceProvider _namedServiceProvider;
        private readonly ConcurrentDictionary<string, IDeliveryClient> _cache = new ConcurrentDictionary<string, IDeliveryClient>();
        private readonly ConcurrentDictionary<string, Func<IDeliveryClientBuilder, IDeliveryClient>> _builders = new ConcurrentDictionary<string, Func<IDeliveryClientBuilder, IDeliveryClient>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClientFactory"/> class.
        /// </summary>
        /// <param name="deliveryOptions">Used for notifications when <see cref="DeliveryOptions"/> instances change.</param>
        /// <param name="serviceProvider">An <see cref="IServiceProvider"/> instance.</param>
        /// <param name="namedServiceProvider">A named service provider.</param>
        [Obsolete("Use other constructor instead.")]
        public NamedDeliveryClientFactory(IOptionsMonitor<DeliveryOptions> deliveryOptions, IServiceProvider serviceProvider,
            INamedServiceProvider namedServiceProvider)
        {
            _deliveryOptions = deliveryOptions;
            _serviceProvider = serviceProvider;
            _namedServiceProvider = namedServiceProvider;
        }

        public NamedDeliveryClientFactory() { }

        /// <inheritdoc />
        public IDeliveryClient Get(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!_cache.TryGetValue(name, out var client))
            {
                if (!_builders.TryGetValue(name, out var builder))
                {
                    // TODO fix this
                    return null;
                }
                // TODO 312 else statements
            }

            return client;
        }


        // TODO log warning for registering client with the same name
        public void Set(string name, Func<IDeliveryClientBuilder, IDeliveryClient> clientBuilder) => _builders.AddOrUpdate(name, clientBuilder, (key, oldValue) => clientBuilder);
        [Obsolete("If you want to use single delivery client use AddDeliveryClient when registering")]
        public IDeliveryClient Get() => GetService<IDeliveryClient>();


        [Obsolete("If you want to use single delivery client use AddDeliveryClient when registering")]
        private T GetService<T>()
        {
            return _serviceProvider.GetService<T>();
        }
    }
}
