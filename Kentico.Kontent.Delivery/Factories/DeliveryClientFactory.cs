using Kentico.Kontent.Delivery.Builders.DeliveryOptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kentico.Kontent.Delivery.Factories
{
    /// <summary>
    /// A factory for <see cref="IDeliveryClient"/>
    /// </summary>
    public class DeliveryClientFactory : IDeliveryClientFactory
    {
        private readonly IOptionsMonitor<DeliveryClientFactoryOptions> _optionsMonitor;
        private readonly ILogger<DeliveryClientFactory> _logger;

        /// <summary>
        /// </summary>
        /// <param name="optionsMonitor">A delivery factory options</param>
        /// <param name="logger">A logger</param>
        public DeliveryClientFactory(IOptionsMonitor<DeliveryClientFactoryOptions> optionsMonitor, ILogger<DeliveryClientFactory> logger)
        {
            _optionsMonitor = optionsMonitor;
            _logger = logger;
        }

        /// <summary>
        /// Create an IDeliveryClient by configuration name
        /// </summary>
        /// <param name="name">A name of <see cref="IDeliveryClient"/> configuration</param>
        /// <returns></returns>
        public IDeliveryClient CreateDeliveryClient(string name)
        {
            if(name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var options = _optionsMonitor.Get(name);
            var client = options.DeliveryClientActions.FirstOrDefault().Invoke();
            return client;
        }
    }
}
