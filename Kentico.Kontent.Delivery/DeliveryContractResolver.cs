using System;
using Newtonsoft.Json.Serialization;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// DI-based contract resolver.
    /// </summary>
    internal class DeliveryContractResolver : DefaultContractResolver
    {
        private IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Instantiates a contract resolver with a DI container.
        /// </summary>
        /// <param name="serviceProvider">Service provider used for loading concrete implementations of interfaces.</param>
        public DeliveryContractResolver(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Creates JSON contracts for interfaces based on type-binding information stored in the DI container.
        /// </summary>
        /// <param name="objectType">Type whose instance should be created.</param>
        /// <returns>Information about how a type should be instantiated.</returns>
        protected override JsonContract CreateContract(Type objectType)
        {
            JsonContract contract = null;
            //TODO: implement contract caching
            if (objectType.IsInterface)
            {
                var service = ServiceProvider.GetService(objectType);
                if (service != null)
                {
                    contract = base.CreateObjectContract(service.GetType());
                    contract.DefaultCreator = () => ServiceProvider.GetService(objectType);
                }
            }
            return contract ?? base.CreateContract(objectType);
        }
    }
}
