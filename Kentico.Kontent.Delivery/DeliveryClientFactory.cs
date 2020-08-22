using System;
using System.Collections.Concurrent;
using System.Linq;
using Kentico.Kontent.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// A factory class for <see cref="IDeliveryClient"/>
    /// </summary>
    public class DeliveryClientFactory : IDeliveryClientFactory
    {
        private readonly IOptionsMonitor<DeliveryClientFactoryOptions> _optionsMonitor;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, IDeliveryClient> _cache = new ConcurrentDictionary<string, IDeliveryClient>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClientFactory"/> class
        /// </summary>
        /// <param name="optionsMonitor">A <see cref="DeliveryClientFactory"/> options</param>
        /// <param name="serviceProvider">A <see cref="IServiceProvider"/> instance</param>
        public DeliveryClientFactory(IOptionsMonitor<DeliveryClientFactoryOptions> optionsMonitor, IServiceProvider serviceProvider)
        {
            _optionsMonitor = optionsMonitor;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Returns a named <see cref="IDeliveryClient"/>.
        /// </summary>
        /// <param name="name">A name of <see cref="IDeliveryClient"/> configuration</param>
        /// <returns>The <see cref="IDeliveryClient"/> instance that represents named client</returns>
        public IDeliveryClient Get(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!_cache.TryGetValue(name, out var client))
            {
                var deliveryClientFactoryOptions = _optionsMonitor.Get(name);
                var deliveryClientOptions = deliveryClientFactoryOptions.DeliveryClientsOptions.LastOrDefault()?.Invoke();
                client = Build(deliveryClientOptions);

                var cacheManagerFactory = _serviceProvider.GetService<IDeliveryCacheManagerFactory>();
                var cacheManager = cacheManagerFactory?.Get(name);
                if (cacheManager != null)
                {
                    var deliveryClientCacheFactory = _serviceProvider.GetService<IDeliveryClientCacheFactory>();
                    client = deliveryClientCacheFactory.Create(cacheManager, client);
                }

                _cache.TryAdd(name, client);
            }

            return client;
        }

        /// <summary>
        /// Returns an <see cref="IDeliveryClient"/>.
        /// </summary>
        /// <returns>The <see cref="IDeliveryClient"/> instance that represents client</returns>
        public IDeliveryClient Get()
        {
            return _serviceProvider.GetRequiredService<IDeliveryClient>();
        }

        private IDeliveryClient Build(DeliveryOptions options)
        {
            return new DeliveryClient(
                Options.Create(options),
                GetService<IModelProvider>(),
                GetService<IRetryPolicyProvider>(),
                GetService<ITypeProvider>(),
                GetService<IDeliveryHttpClient>(),
                GetService<JsonSerializer>());
        }

        private T GetService<T>()
        {
            return _serviceProvider.GetService<T>();
        }
    }
}
