using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Kentico.Kontent.Delivery.Caching.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDeliveryClientCache(this IServiceCollection services, DeliveryCacheOptions options)
        {
            return services
                 .RegisterCacheOptions(options)
                 .RegisterDependencis()
                 .Decorate<IDeliveryClient, DeliveryClientCache>();
        }
        public static IServiceCollection AddDeliveryClientCache(this IServiceCollection services, string name, DeliveryCacheOptions options)
        {
            services
                .RegisterCacheOptions(options)
                .RegisterDependencis()
                .AddTransient<IConfigureOptions<DeliveryClientFactoryOptions>>(sp =>
                {
                    return new ConfigureNamedOptions<DeliveryClientFactoryOptions>(name, o =>
                    {
                        var client = o.DeliveryClientsActions.FirstOrDefault()?.Invoke();
                        o.DeliveryClientsActions.Add(() =>
                        {
                            var serviceProvider = services.BuildServiceProvider();
                            var deliveryCacheManager = serviceProvider.GetRequiredService<IDeliveryCacheManager>();
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

        private static IServiceCollection RegisterDependencis(this IServiceCollection services)
        {
            services.TryAddSingleton<IDeliveryCacheManager, DeliveryCacheManager>();
            services.TryAddSingleton<IMemoryCache, MemoryCache>();

            return services;
        }

    }
}
