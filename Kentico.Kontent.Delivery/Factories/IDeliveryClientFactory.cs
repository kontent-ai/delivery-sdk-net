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
        /// Returns a named <see cref="IDeliveryClient"/> class.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IDeliveryClient Get(string name);

        /// <summary>
        /// Returns a default implementation of <see cref="IDeliveryClient"/> class.
        /// </summary>
        /// <returns></returns>
        IDeliveryClient Get();
    }
}
