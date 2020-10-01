using System;
using System.Collections.Concurrent;
using System.Linq;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Configuration;
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
        private readonly IOptionsMonitor<DeliveryOptions> _deliveryOptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, IDeliveryClient> _cache = new ConcurrentDictionary<string, IDeliveryClient>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClientFactory"/> class
        /// </summary>
        /// <param name="deliveryOptions">Used for notifications when <see cref="DeliveryOptions"/> instances change.</param>
        /// <param name="serviceProvider">A <see cref="IServiceProvider"/> instance</param>
        public DeliveryClientFactory(IOptionsMonitor<DeliveryOptions> deliveryOptions, IServiceProvider serviceProvider)
        {
            _deliveryOptions = deliveryOptions;
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
                var deliveryClientOptions = _deliveryOptions.Get(name);

                // Validate that the option object is indeed configured
                if (deliveryClientOptions.ProjectId != null)
                {
                    client = Build(deliveryClientOptions, name);

                    _cache.TryAdd(name, client);
                }
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

        private IDeliveryClient Build(DeliveryOptions options, string name)
        {
            return new DeliveryClient(
                new DeliveryOptionsMonitor(options, name),
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
