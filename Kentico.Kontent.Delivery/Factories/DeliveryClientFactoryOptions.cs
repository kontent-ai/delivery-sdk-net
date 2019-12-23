using Kentico.Kontent.Delivery.Builders.DeliveryOptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kentico.Kontent.Delivery.Factories
{
    /// <summary>
    /// An options class for configuring the default <see cref="DeliveryClientFactory"/>.
    /// </summary>
    public class DeliveryClientFactoryOptions
    {
        /// <summary>
        /// Gets a list of operations used to configure an <see cref="IDeliveryClient"/>.
        /// </summary>
        public IList<Func<IDeliveryClient>> DeliveryClientActions { get; } = new List<Func<IDeliveryClient>>();
    }
}
