using System;
using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// Collection of all types from <see cref="Kentico.Kontent.Delivery"/> registered as implemented interfaces.
    /// </summary>
    public class DeliveryServiceCollection
    {
        /// <summary>
        /// Provider able to resolve any concrete type based on any interface from <see cref="Abstractions"/>.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Creates an instance of the service collection.
        /// </summary>
        public DeliveryServiceCollection()
        {
            var collection = new ServiceCollection();
            // Load and register concrete implementations of all interfaces and creates bindings between them <IType, Type>
            collection.Scan(scan => scan.FromAssemblyOf<DeliveryClient>().AddClasses(false).AsImplementedInterfaces());
            ServiceProvider = collection.BuildServiceProvider();
        }
    }
}
