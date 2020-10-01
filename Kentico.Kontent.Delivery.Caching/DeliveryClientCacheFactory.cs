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
        private readonly IDeliveryClientFactory _clientFactory;
        private readonly IOptionsMonitor<DeliveryCacheOptions> _deliveryCacheOptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, IDeliveryClient> _cache = new ConcurrentDictionary<string, IDeliveryClient>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClientCacheFactory"/> class
        /// </summary>
        /// <param name="clientFactory">Factory to be decorated</param>
        /// <param name="deliveryCacheOptions">Cache configuration options</param>
        /// <param name="serviceProvider">An <see cref="IServiceProvider"/> instance</param>
        /// 
        public DeliveryClientCacheFactory(IDeliveryClientFactory clientFactory, IOptionsMonitor<DeliveryCacheOptions> deliveryCacheOptions, IServiceProvider serviceProvider)
        {
            _clientFactory = clientFactory;
            _deliveryCacheOptions = deliveryCacheOptions;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Returns a named <see cref="IDeliveryCacheManager"/>.
        /// </summary>
        /// <param name="name">A name of <see cref="IDeliveryCacheManager"/> configuration</param>
        /// <returns>The <see cref="IDeliveryCacheManager"/> instance that represents named cache manager</returns>
        public IDeliveryClient Get(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!_cache.TryGetValue(name, out var client))
            {
                var cacheOptions = _deliveryCacheOptions.Get(name);
                if (cacheOptions.Name == name)
                {
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

                    client = new DeliveryClientCache(manager, _clientFactory.Get(name));

                    _cache.TryAdd(name, client);
                }
            }

            return client;
        }

        /// <inheritdoc />
        public IDeliveryClient Get()
        {
            return _clientFactory.Get();
        }
    }
}
