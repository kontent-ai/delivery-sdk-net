using Kentico.Kontent.Delivery.Builders.DeliveryOptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Kontent.Delivery.Factories
{
    /// <summary>
    /// A factory for <see cref="IDeliveryClient"/>
    /// </summary>
    public class DeliveryClientFactory : IDeliveryClientFactory
    {
        private readonly IOptionsMonitor<DeliveryClientFactoryOptions> _optionsMonitor;
        private readonly ILogger<DeliveryClientFactory> _logger;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// </summary>
        /// <param name="optionsMonitor">A <see cref="DeliveryClientFactory"/> options</param>
        /// <param name="logger">A logger</param>
        /// <param name="serviceProvider">A ServiceProvider implementation</param>
        public DeliveryClientFactory(IOptionsMonitor<DeliveryClientFactoryOptions> optionsMonitor, ILogger<DeliveryClientFactory> logger, IServiceProvider serviceProvider)
        {
            _optionsMonitor = optionsMonitor;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Create a IDeliveryClient by configuration name
        /// </summary>
        /// <param name="name">A name of <see cref="IDeliveryClient"/> configuration</param>
        /// <returns></returns>
        public IDeliveryClient CreateDeliveryClient(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var options = _optionsMonitor.Get(name);
            var client = options.DeliveryClientActions.FirstOrDefault()?.Invoke();
            return client;
        }

        /// <summary>
        /// Create a default IDeliveryClient
        /// </summary>
        /// <returns></returns>
        public IDeliveryClient CreateDeliveryClient()
        {
            return _serviceProvider.GetRequiredService<IDeliveryClient>();
        }
    }
}
