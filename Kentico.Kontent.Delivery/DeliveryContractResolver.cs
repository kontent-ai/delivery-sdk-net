using System;
using System.Collections.Generic;
using System.Linq;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentTypes.Element;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// DI-based contract resolver.
    /// </summary>
    internal class DeliveryContractResolver : DefaultContractResolver
    {
        private readonly Dictionary<Type, JsonContract> _contractCache = new Dictionary<Type, JsonContract>();

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

            if (objectType.IsInterface)
            {
                if (!_contractCache.TryGetValue(objectType, out contract))
                {
                    var services = ServiceProvider.GetServices(objectType);
                    if (services != null && services.Any())
                    {
                        var implementation = GetClosestImplementation(services, objectType);
                        contract = base.CreateObjectContract(implementation);
                        if (objectType.IsAssignableFrom(typeof(IContentElement)))
                        {
                            contract.Converter = new ContentElementConverter();
                        }
                        contract.DefaultCreator = () => ServiceProvider.GetService(implementation);
                        _contractCache.Add(objectType, contract);
                    }
                }
            }
            return contract ?? base.CreateContract(objectType);
        }

        public Type GetClosestImplementation(IEnumerable<object> services, Type @interface)
            => services.Select(s => s.GetType()).FirstOrDefault(type => type.GetInterfaces().Contains(@interface));
    }
}
