using System;
using System.Collections.Generic;
using System.Text;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Defines a methods for getting a <see cref="IDeliveryCacheManager"/>
    /// </summary>
    public interface IDeliveryCacheManagerFactory
    {
        /// <summary>
        /// Returns a named <see cref="IDeliveryCacheManager"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>The <see cref="IDeliveryCacheManager"/> instance that represents named cache manager</returns>
        IDeliveryCacheManager Get(string name);
    }
}
