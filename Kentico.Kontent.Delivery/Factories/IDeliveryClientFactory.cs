using System;
using System.Collections.Generic;
using System.Text;

namespace Kentico.Kontent.Delivery.Factories
{
    /// <summary>
    /// Defines a methods for creating a new Delivery client.
    /// </summary>
    public interface IDeliveryClientFactory
    {
        /// <summary>
        /// Returns a named <see cref="IDeliveryClient"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IDeliveryClient Get(string name);

        /// <summary>
        /// Returns an <see cref="IDeliveryClient"/>.
        /// </summary>
        /// <returns></returns>
        IDeliveryClient Get();
    }
}
