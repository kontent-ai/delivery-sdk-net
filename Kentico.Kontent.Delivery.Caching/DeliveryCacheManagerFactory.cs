using Kentico.Kontent.Delivery.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text;

namespace Kentico.Kontent.Delivery.Caching
{
    /// <summary>
    /// A factory class for <see cref="IDeliveryCacheManager"/>
    /// </summary>
    public class DeliveryCacheManagerFactory : IDeliveryCacheManagerFactory
    {
        private readonly IOptionsMonitor<DeliveryCacheManagerFactoryOptions> _optionsMonitor;
        private readonly IServiceProvider _serviceProvider;

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

            IDeliveryCacheManager manager = null;
            var deliveryClientCacheFactoryOptions = _optionsMonitor.Get(name);

            if (deliveryClientCacheFactoryOptions != null)
            {
                var deliveryClientCacheOptions = deliveryClientCacheFactoryOptions.DeliveryCacheOptions.LastOrDefault()?.Invoke();

                if (deliveryClientCacheOptions?.CacheType == CacheTypeEnum.Memory)
                {
                    var memoryCache = _serviceProvider.GetService<IMemoryCache>();
                    manager = new MemoryCacheManager(memoryCache, Options.Create(deliveryClientCacheOptions));
                }
                else
                {
                    var distributedCache = _serviceProvider.GetService<IDistributedCache>();
                    manager = new DistributedCacheManager(distributedCache, Options.Create(deliveryClientCacheOptions));
                }
            }

            return manager;
        }

    }
}
