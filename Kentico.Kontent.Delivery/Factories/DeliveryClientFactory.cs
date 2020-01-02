using Kentico.Kontent.Delivery.Builders.DeliveryOptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Kentico.Kontent.Delivery.Factories
{
    /// <summary>
    /// A factory for <see cref="IDeliveryClient"/>
    /// </summary>
    public class DeliveryClientFactory : IDeliveryClientFactory
    {
        private ConcurrentDictionary<string, IDeliveryClient> cachedDeliveryClients = new ConcurrentDictionary<string, IDeliveryClient>();
        private readonly IOptionsMonitor<DeliveryClientFactoryOptions> _optionsMonitor;
        private readonly ILogger<DeliveryClientFactory> _logger;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// </summary>
        /// <param name="optionsMonitor">A <see cref="DeliveryClientFactory"/> options</param>
        /// <param name="logger">A logger</param>
        /// <param name="serviceProvider">An IServiceProvider implementation</param>
        public DeliveryClientFactory(IOptionsMonitor<DeliveryClientFactoryOptions> optionsMonitor, ILogger<DeliveryClientFactory> logger, IServiceProvider serviceProvider)
        {
            _optionsMonitor = optionsMonitor;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Returns a named <see cref="IDeliveryClient"/>.
        /// </summary>
        /// <param name="name">A name of <see cref="IDeliveryClient"/> configuration</param>
        /// <returns></returns>
        public IDeliveryClient Get(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if(!cachedDeliveryClients.TryGetValue(name, out var client))
            {
                var options = _optionsMonitor.Get(name);
                client = options.DeliveryClientActions.FirstOrDefault()?.Invoke();
                cachedDeliveryClients.TryAdd(name, client);
            }
          
            return client;
        }

        /// <summary>
        /// Returns an <see cref="IDeliveryClient"/>.
        /// </summary>
        /// <returns></returns>
        public IDeliveryClient Get()
        {
            return _serviceProvider.GetRequiredService<IDeliveryClient>();
        }
    }
}
