using System;
using System.Linq;
using Kentico.Kontent.Delivery.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Kentico.Kontent.Delivery.Caching.Extensions
{
    /// <summary>
    /// A class which contains extension methods on <see cref="IServiceCollection"/> for registering an cached <see cref="IDeliveryClient"/> instance.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a delegate that will be used to configure a cached <see cref="IDeliveryClient"/>.
        /// </summary>
        /// <param name="services">A <see cref="IServiceCollection"/> instance for registering and resolving dependencies.</param>
        /// <param name="options">A <see cref="DeliveryCacheOptions"/> instance.</param>
        /// <returns>The <paramref name="services"/> instance with cache services registered in it</returns>
        public static IServiceCollection AddDeliveryClientCache(this IServiceCollection services, DeliveryCacheOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), "The Delivery cache  options object is not specified.");
            }

            return services
                 .RegisterCacheOptions(options)
                 .RegisterDependencies()
                 .Decorate<IDeliveryClient, DeliveryClientCache>();
        }

        /// <summary>
        ///  Registers a delegate that will be used to configure a cached <see cref="IDeliveryClient"/>.
        /// </summary>
        /// <param name="services">A <see cref="IServiceCollection"/> instance for registering and resolving dependencies.</param>
        /// <param name="name">A name of named client which want to use cached <see cref="IDeliveryClient"/></param>
        /// <param name="options">A <see cref="DeliveryCacheOptions"/> instance. </param> 
        /// <returns>The <paramref name="services"/> instance with cache services registered in it</returns>
        public static IServiceCollection AddDeliveryClientCache(this IServiceCollection services, string name, DeliveryCacheOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), "The Delivery cache  options object is not specified.");
            }

            services
                .RegisterCacheOptions(options)
                .RegisterDependencies()
                .AddTransient<IConfigureOptions<DeliveryClientFactoryOptions>>(sp =>
                {
                    return new ConfigureNamedOptions<DeliveryClientFactoryOptions>(name, o =>
                    {
                        var client = o.DeliveryClientsActions.FirstOrDefault()?.Invoke();
                        o.DeliveryClientsActions.Add(() =>
                        {
                            var deliveryCacheManager = sp.GetRequiredService<IDeliveryCacheManager>();
                            return new DeliveryClientCache(deliveryCacheManager, client);
                        });
                    });

                });

            return services;
        }

        private static IServiceCollection RegisterCacheOptions(this IServiceCollection services, DeliveryCacheOptions options)
        {
            services.Configure<DeliveryCacheOptions>(o =>
            {
                o.DefaultExpiration = options.DefaultExpiration;
                o.StaleContentExpiration = options.StaleContentExpiration;
            });
            return services;
        }

        private static IServiceCollection RegisterDependencies(this IServiceCollection services)
        {
            services.TryAddSingleton<IDeliveryCacheManager, DeliveryCacheManager>();
            services.TryAddSingleton<IMemoryCache, MemoryCache>();

            return services;
        }
    }
}
