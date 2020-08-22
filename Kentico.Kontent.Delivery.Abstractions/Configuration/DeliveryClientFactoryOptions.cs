using System;
using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents configuration of the <see cref="IDeliveryClientFactory"/>.
    /// </summary>
    public class DeliveryClientFactoryOptions
    {
        /// <summary>
        /// Gets a list of options used to configure an <see cref="IDeliveryClient"/>.
        /// </summary>
        public IList<Func<DeliveryOptions>> DeliveryClientsOptions { get; } = new List<Func<DeliveryOptions>>();
    }
}
