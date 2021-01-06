using System;
using Kentico.Kontent.Delivery.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// A factory class for <see cref="IDeliveryClient"/>
    /// </summary>
    public sealed class DeliveryClientFactory : IDeliveryClientFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryClientFactory"/> class.
        /// </summary>
        /// <param name="serviceProvider">An <see cref="IServiceProvider"/> instance.</param>
        public DeliveryClientFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public IDeliveryClient Get(string name) => null;

        /// <inheritdoc />	
        public IDeliveryClient Get()
        {
            return _serviceProvider.GetRequiredService<IDeliveryClient>();
        }

        /// <summary>
        /// Creates a <see cref="IDeliveryClient"/> instance manually.
        /// </summary>
        /// <param name="options">A <see cref="DeliveryOptions"/></param>
        /// <param name="modelProvider">A <see cref="IModelProvider"/> instance.</param>
        /// <param name="retryPolicyProvider">A <see cref="IRetryPolicyProvider"/> instance.</param>
        /// <param name="typeProvider">A <see cref="ITypeProvider"/> instance.</param>
        /// <param name="deliveryHttpClient">A <see cref="IDeliveryHttpClient"/> instance.</param>
        /// <param name="jsonSerializer">A <see cref="JsonSerializer"/> instance.</param>
        /// <returns></returns>
        public static IDeliveryClient Create(
            IOptionsMonitor<DeliveryOptions> options,
            IModelProvider modelProvider,
            IRetryPolicyProvider retryPolicyProvider,
            ITypeProvider typeProvider,
            IDeliveryHttpClient deliveryHttpClient,
            JsonSerializer jsonSerializer)
        {
            return new DeliveryClient(options,
                modelProvider,
                retryPolicyProvider,
                typeProvider,
                deliveryHttpClient,
                jsonSerializer);
        }
    }
}
