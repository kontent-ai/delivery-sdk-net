using Kentico.Kontent.Delivery.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kentico.Kontent.Delivery.Configuration
{
    /// <summary>
    /// Represents configuration of the <see cref="DeliveryClientFactory"/>.
    /// </summary>
    public class DeliveryClientFactoryOptions
    {
        /// <summary>
        /// Gets a list of operations used to configure an <see cref="IDeliveryClient"/>.
        /// </summary>
        public IList<Func<IDeliveryClient>> DeliveryClientsActions { get; } = new List<Func<IDeliveryClient>>();
    }
}
