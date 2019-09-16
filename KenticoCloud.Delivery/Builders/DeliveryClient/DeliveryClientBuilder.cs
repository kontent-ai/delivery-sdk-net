using System;
using KenticoKontent.Delivery.Builders.DeliveryClient;
using KenticoKontent.Delivery.Builders.DeliveryOptions;

namespace KenticoKontent.Delivery
{
    /// <summary>
    /// A builder class for creating an instance of the <see cref="IDeliveryClient"/> interface.
    /// </summary>
    public sealed class DeliveryClientBuilder
    {
        private static IDeliveryClientBuilder Builder => new DeliveryClientBuilderImplementation();

        /// <summary>
        /// Mandatory step of the <see cref="DeliveryClientBuilder"/> for specifying Kentico Kontent project id.
        /// </summary>
        /// <param name="projectId">The identifier of the Kentico Kontent project.</param>
        public static IOptionalClientSetup WithProjectId(string projectId)
            => Builder.BuildWithProjectId(projectId);

        /// <summary>
        /// Mandatory step of the <see cref="DeliveryClientBuilder"/> for specifying Kentico Kontent project id.
        /// </summary>
        /// <param name="projectId">The identifier of the Kentico Kontent project.</param>
        public static IOptionalClientSetup WithProjectId(Guid projectId)
            => Builder.BuildWithProjectId(projectId);

        /// <summary>
        /// Mandatory step of the <see cref="DeliveryClientBuilder"/> for specifying Kentico Kontent project settings.
        /// </summary>
        /// <param name="buildDeliveryOptions">A function that is provided with an instance of <see cref="DeliveryOptionsBuilder"/> and expected to return a valid instance of <see cref="DeliveryOptions"/>.</param>
        public static IOptionalClientSetup WithOptions(Func<IDeliveryOptionsBuilder, DeliveryOptions> buildDeliveryOptions)
            => Builder.BuildWithDeliveryOptions(buildDeliveryOptions);
    }
}
