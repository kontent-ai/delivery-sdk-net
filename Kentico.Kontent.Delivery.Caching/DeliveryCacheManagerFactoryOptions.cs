using Kentico.Kontent.Delivery.Abstractions;
using System;
using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Caching
{
    /// <summary>
    /// Represents configuration of the <see cref="DeliveryCacheManagerFactory"/>.
    /// </summary>
    public class DeliveryCacheManagerFactoryOptions
    {
        /// <summary>
        /// Gets a list of options used to configure an <see cref="IDeliveryCacheManager"/>.
        /// </summary>
        public IList<Func<DeliveryCacheOptions>> DeliveryCacheOptions { get; } = new List<Func<DeliveryCacheOptions>>();
    }
}
