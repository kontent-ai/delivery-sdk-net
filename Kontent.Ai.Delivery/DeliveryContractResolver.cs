using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentTypes.Element;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;

namespace Kontent.Ai.Delivery
{
    /// <summary>
    /// DI-based contract resolver.
    /// </summary>
    internal class DeliveryContractResolver : DefaultContractResolver
    {
        private readonly ConcurrentDictionary<Type, JsonContract> _contractCache = new ConcurrentDictionary<Type, JsonContract>();

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
            if (!_contractCache.TryGetValue(objectType, out var contract))
            {
                if (objectType.IsInterface)
                {
                    var services = ServiceProvider.GetServices(objectType);
                    if (services != null)
                    {
                        var implementation = GetClosestImplementation(services, objectType);
                        if (implementation != null)
                        {
                            contract = base.CreateObjectContract(implementation);
                            if (objectType.IsAssignableFrom(typeof(IContentElement)))
                            {
                                contract.Converter = new ContentElementConverter();
                            }

                            contract.DefaultCreator = () => ServiceProvider.GetService(implementation);
                        }
                    }
                }

                contract ??= base.CreateContract(objectType);
                _contractCache.TryAdd(objectType, contract);
            }

            return contract;
        }

        public Type GetClosestImplementation(IEnumerable<object> services, Type @interface)
            => services.Select(s => s.GetType()).FirstOrDefault(type => type.GetInterfaces().Contains(@interface));
    }
}
