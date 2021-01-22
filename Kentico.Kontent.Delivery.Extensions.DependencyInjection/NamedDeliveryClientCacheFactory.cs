using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Caching;
using Kentico.Kontent.Delivery.Caching.Factories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection
{
    internal class NamedDeliveryClientCacheFactory : IDeliveryClientFactory
    {
        private readonly IOptionsMonitor<DeliveryCacheOptions> _deliveryCacheOptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly INamedServiceProvider _customServiceProvider;
        private readonly IDeliveryClientFactory _innerDeliveryClientFactory;
        private readonly ConcurrentDictionary<string, IDeliveryClient> _cache = new ConcurrentDictionary<string, IDeliveryClient>();

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedDeliveryClientCacheFactory"/> class.
        /// </summary>
        /// <param name="deliveryClientFactory">Factory to be decorated.</param>
        /// <param name="deliveryCacheOptions">Cache configuration options.</param>
        /// <param name="serviceProvider">An <see cref="IServiceProvider"/> instance.</param>
        /// <param name="customServiceProvider">A custom service provider.</param>
        public NamedDeliveryClientCacheFactory(IDeliveryClientFactory deliveryClientFactory, IOptionsMonitor<DeliveryCacheOptions> deliveryCacheOptions, IServiceProvider serviceProvider, INamedServiceProvider customServiceProvider)
        {
            _deliveryCacheOptions = deliveryCacheOptions;
            _serviceProvider = serviceProvider;
            _customServiceProvider = customServiceProvider;
            _innerDeliveryClientFactory = deliveryClientFactory;
        }

        public IDeliveryClient Get(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!_cache.TryGetValue(name, out var client))
            {
                client = _innerDeliveryClientFactory.Get(name);
                if (client != null)
                {
                    var cacheOptions = _deliveryCacheOptions.Get(name);
                    if (cacheOptions.Name == name)
                    {
                        // Build caching services according to the options
                        IDeliveryCacheManager manager;
                        if (cacheOptions.CacheType == CacheTypeEnum.Memory)
                        {
                            var memoryCache = GetNamedServiceOrDefault<IMemoryCache>(name);
                            manager = CacheManagerFactory.Create(memoryCache, Options.Create(cacheOptions));
                        }
                        else
                        {
                            var distributedCache = GetNamedServiceOrDefault<IDistributedCache>(name);
                            manager = CacheManagerFactory.Create(distributedCache, Options.Create(cacheOptions));
                        }

                        // Decorate the client with a caching layer
                        client = new DeliveryClientCache(manager, client);

                        _cache.TryAdd(name, client);
                    }
                }
            }

            return client;
        }

        public IDeliveryClient Get() => _innerDeliveryClientFactory.Get();

        private T GetNamedServiceOrDefault<T>(string name)
        {
            var service = _customServiceProvider.GetService<T>(name);
            if (service == null)
            {
                service = _serviceProvider.GetService<T>();
            }

            return service;
        }
    }
}
