using Kentico.Kontent.Delivery.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Kentico.Kontent.Delivery.Caching
{
    /// <summary>
    /// A factory class for <see cref="IDeliveryCacheManager"/>
    /// </summary>
    public class DeliveryCacheManagerFactory : IDeliveryCacheManagerFactory
    {
        private readonly IOptionsMonitor<DeliveryCacheManagerFactoryOptions> _optionsMonitor;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, IDeliveryCacheManager> _cache = new ConcurrentDictionary<string, IDeliveryCacheManager>();
        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryCacheManagerFactory"/> class
        /// </summary>
        /// <param name="optionsMonitor">A <see cref="DeliveryCacheManagerFactory"/> options</param>
        /// <param name="serviceProvider">A <see cref="IServiceProvider"/> instance</param>
        public DeliveryCacheManagerFactory(IOptionsMonitor<DeliveryCacheManagerFactoryOptions> optionsMonitor, IServiceProvider serviceProvider)
        {
            _optionsMonitor = optionsMonitor;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Returns a named <see cref="IDeliveryCacheManager"/>.
        /// </summary>
        /// <param name="name">A name of <see cref="IDeliveryCacheManager"/> configuration</param>
        /// <returns>The <see cref="IDeliveryCacheManager"/> instance that represents named cache manager</returns>
        public IDeliveryCacheManager Get(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!_cache.TryGetValue(name, out var manager))
            {
                var deliveryCacheManagerFactoryOptions = _optionsMonitor.Get(name);
                var deliveryCacheOptions = deliveryCacheManagerFactoryOptions?.DeliveryCacheOptions.LastOrDefault()?.Invoke();
                if (deliveryCacheOptions != null)
                {
                    if (deliveryCacheOptions.CacheType == CacheTypeEnum.Memory)
                    {
                        var memoryCache = _serviceProvider.GetService<IMemoryCache>();
                        manager = new MemoryCacheManager(memoryCache, Options.Create(deliveryCacheOptions));
                    }
                    else
                    {
                        var distributedCache = _serviceProvider.GetService<IDistributedCache>();
                        manager = new DistributedCacheManager(distributedCache, Options.Create(deliveryCacheOptions));
                    }

                    _cache.TryAdd(name, manager);
                }
            }

            return manager;
        }

        /// <summary>
        /// Returns a <see cref="IDeliveryCacheManager"/>.
        /// </summary>
        /// <returns>The <see cref="IDeliveryCacheManager"/> instance that represents cache manager</returns>
        public IDeliveryCacheManager Get()
        {
            return _serviceProvider.GetRequiredService<IDeliveryCacheManager>();
        }
    }
}
