using Kentico.Kontent.Delivery.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;

namespace Kentico.Kontent.Delivery.Caching
{
    /// <summary>
    /// A factory class for <see cref="IDeliveryCacheManager"/>
    /// </summary>
    public class DeliveryClientCacheFactory : IDeliveryClientFactory
    {
        private readonly IDeliveryClientFactory _innerClientFactory;
        private readonly IOptionsMonitor<DeliveryCacheOptions> _deliveryCacheOptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, IDeliveryClient> _cache = new ConcurrentDictionary<string, IDeliveryClient>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClientCacheFactory"/> class.
        /// </summary>
        /// <param name="clientFactory">Factory to be decorated.</param>
        /// <param name="deliveryCacheOptions">Cache configuration options.</param>
        /// <param name="serviceProvider">An <see cref="IServiceProvider"/> instance.</param>
        public DeliveryClientCacheFactory(IDeliveryClientFactory clientFactory, IOptionsMonitor<DeliveryCacheOptions> deliveryCacheOptions, IServiceProvider serviceProvider)
        {
            _innerClientFactory = clientFactory;
            _deliveryCacheOptions = deliveryCacheOptions;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Returns a named instance of the <see cref="IDeliveryClient"/> wrapped in a caching layer.
        /// </summary>
        /// <param name="name">A name of the configuration to be used to instantiate the client.</param>
        /// <returns>Returns an <see cref="IDeliveryClient"/> instance with the given name.</returns>
        public IDeliveryClient Get(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!_cache.TryGetValue(name, out var client))
            {
                client = _innerClientFactory.Get(name);
                var cacheOptions = _deliveryCacheOptions.Get(name);
                if (cacheOptions.Name == name)
                {
                    // Build caching services according to the options
                    IDeliveryCacheManager manager;
                    if (cacheOptions.CacheType == CacheTypeEnum.Memory)
                    {
                        var memoryCache = _serviceProvider.GetService<IMemoryCache>();
                        manager = new MemoryCacheManager(memoryCache, Options.Create(cacheOptions));
                    }
                    else
                    {
                        var distributedCache = _serviceProvider.GetService<IDistributedCache>();
                        manager = new DistributedCacheManager(distributedCache, Options.Create(cacheOptions));
                    }

                    // Decorate the client with a caching layer
                    client = new DeliveryClientCache(manager, client);

                    _cache.TryAdd(name, client);
                }
            }

            return client;
        }

        /// <inheritdoc />
        public IDeliveryClient Get()
        {
            return _innerClientFactory.Get();
        }
    }
}
