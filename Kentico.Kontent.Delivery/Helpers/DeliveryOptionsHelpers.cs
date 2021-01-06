using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Configuration;
using System;

namespace Kentico.Kontent.Delivery.Helpers
{
    /// <summary>
    /// Helpers for a <see cref="DeliveryOptions"/>
    /// </summary>
    public static class DeliveryOptionsHelpers
    {
        /// <summary>
        /// Builds the <see cref="DeliveryOptions"/> instance from the delegate.
        /// </summary>
        /// <param name="buildDeliveryOptions"></param>
        /// <returns></returns>
        public static DeliveryOptions Build(Func<IDeliveryOptionsBuilder, DeliveryOptions> buildDeliveryOptions)
        {
            var builder = DeliveryOptionsBuilder.CreateInstance();
            return buildDeliveryOptions(builder);
        }
    }
}
