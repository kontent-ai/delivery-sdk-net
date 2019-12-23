using System;
using System.Collections.Generic;
using System.Text;

namespace Kentico.Kontent.Delivery.Factories
{
    /// <summary>
    /// Defines a methods for creating new delivery client.
    /// </summary>
    public interface IDeliveryClientFactory
    {
        /// <summary>
        /// Returns a new <see cref="IDeliveryClient"/> class.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IDeliveryClient CreateDeliveryClient(string name);
    }
}
