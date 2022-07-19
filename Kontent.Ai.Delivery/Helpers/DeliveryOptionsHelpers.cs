using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Configuration;
using System;

namespace Kontent.Ai.Delivery.Helpers
{
    /// <summary>
    /// A class which contains helper methods on <see cref="DeliveryOptions"/>.
    /// </summary>
    public static class DeliveryOptionsHelpers
    {
        /// <summary>
        /// Builds the <see cref="DeliveryOptions"/> instance from the delegate.
        /// </summary>
        /// <param name="buildDeliveryOptions">A delegate which returns a <see cref="DeliveryOptions"/> instance.</param>
        /// <returns></returns>
        public static DeliveryOptions Build(Func<IDeliveryOptionsBuilder, DeliveryOptions> buildDeliveryOptions)
        {
            var builder = DeliveryOptionsBuilder.CreateInstance();
            return buildDeliveryOptions(builder);
        }
    }
}
