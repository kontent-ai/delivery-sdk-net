using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
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
                var services = ServiceProvider.GetServices(objectType);
                if (services != null && services.Any())
                {
                    var implementation = GetClosestImplementation(services, objectType);
                    contract = base.CreateObjectContract(implementation);
                    contract.DefaultCreator = () => ServiceProvider.GetService(implementation);
                }
            }
            return contract ?? base.CreateContract(objectType);
        }

        public Type GetClosestImplementation(IEnumerable<object> services, Type interf)
            => services.Select(s => s.GetType()).FirstOrDefault(type => type.GetInterfaces().Contains(interf));

        public Type ClosestAncestor(Type typeOfClass, Type parent)
        {
            var baseType = typeOfClass.BaseType;
            if (typeOfClass.GetInterfaces().Contains(parent) &&
                !baseType.GetInterfaces().Contains(parent))
            {
                return typeOfClass;
            }

            return ClosestAncestor(baseType, parent);
        }
    }
}
