using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Kentico.Kontent.Delivery.Caching.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDeliveryClientCache(this IServiceCollection services, DeliveryCacheOptions options)
        {
            return services
                 .RegisterCacheOptions(options)
                 .RegisterDependencis()
                 .AddSingleton<IDeliveryClient>(sp =>
                 {
                     var deliveryClient = sp.GetRequiredService<IDeliveryClient>();
                     var deliveryCacheManager = sp.GetRequiredService<IDeliveryCacheManager>();
                     return new DeliveryClientCache(deliveryCacheManager, deliveryClient);
                 });
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
                        o.DeliveryClientsActions.Add(() =>
                        {
                            var deliveryClient = sp.GetRequiredService<IDeliveryClient>();
                            var deliveryCacheManager = sp.GetRequiredService<IDeliveryCacheManager>();
                            return new DeliveryClientCache(deliveryCacheManager, deliveryClient);
                        });
                    });

                });

            return services;
        }

        public static IServiceCollection RegisterCacheOptions(this IServiceCollection services, DeliveryCacheOptions options)
        {
            services.Configure<DeliveryCacheOptions>(o =>
            {
                o.DefaultExpiration = options.DefaultExpiration;
                o.StaleContentExpiration = options.StaleContentExpiration;
            });
            return services;
        }

        public static IServiceCollection RegisterDependencis(this IServiceCollection services)
        {
            services.TryAddSingleton<IDeliveryCacheManager, DeliveryCacheManager>();
            services.TryAddSingleton<IMemoryCache, MemoryCache>();

            return services;
        }

    }
}
