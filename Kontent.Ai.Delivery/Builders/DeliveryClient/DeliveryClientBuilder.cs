using System;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Configuration;

namespace Kontent.Ai.Delivery.Builders.DeliveryClient
{
    /// <summary>
    /// A builder of <see cref="IDeliveryClient"/> instances.
    /// </summary>
    public sealed class DeliveryClientBuilder
    {
        private static IDeliveryClientBuilder Builder => new DeliveryClientBuilderImplementation();

        /// <summary>
        /// Use environment identifier.
        /// </summary>
        /// <param name="environmentId">The identifier of a Kontent.ai environment.</param>
        public static IOptionalClientSetup WithEnvironmentId(string environmentId)
            => Builder.BuildWithEnvironmentId(environmentId);

        /// <summary>
        /// Use environment identifier.
        /// </summary>
        /// <param name="environmentId">The identifier of a Kontent.ai environment.</param>
        public static IOptionalClientSetup WithEnvironmentId(Guid environmentId)
            => Builder.BuildWithEnvironmentId(environmentId);

        /// <summary>
        /// Use additional configuration.
        /// </summary>
        /// <param name="buildDeliveryOptions">A delegate that creates an instance of the <see cref="DeliveryOptions"/> using the specified <see cref="DeliveryOptionsBuilder"/>.</param>
        public static IOptionalClientSetup WithOptions(Func<IDeliveryOptionsBuilder, DeliveryOptions> buildDeliveryOptions)
            => Builder.BuildWithDeliveryOptions(buildDeliveryOptions);
    }
}
