using Kentico.Kontent.Delivery.Builders.DeliveryOptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kentico.Kontent.Delivery.Factories
{
    public class DeliveryClientFactory : IDeliveryClientFactory
    {
        private readonly IOptionsMonitor<DeliveryClientFactoryOptions> _optionsMonitor;
        private readonly ILogger<DeliveryClientFactory> _logger;

        public DeliveryClientFactory(IOptionsMonitor<DeliveryClientFactoryOptions> optionsMonitor, ILogger<DeliveryClientFactory> logger)
        {
            _optionsMonitor = optionsMonitor;
            _logger = logger;
        }


        public IDeliveryClient CreateDeliveryClient(string name)
        {
            if(name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var options = _optionsMonitor.Get(name);
            //var client = (IDeliveryClient)new EmptyDeliveryClient(); // TODO BAD
            var client = options.DeliveryClientActions.FirstOrDefault()();
            return client;
        }
    }
}
