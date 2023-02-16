using System;
using Kontent.Ai.Delivery.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kontent.Ai.Delivery.Caching.Extensions
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
        public static IServiceCollection AddDeliveryClientCache(this IServiceCollection services,
            DeliveryCacheOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), "The Delivery cache  options object is not specified.");
            }

            return services
                 .Configure<DeliveryCacheOptions>((o) => o.Configure(options))
                 .RegisterDependencies(options.CacheType)
                 .Decorate<IDeliveryClient, DeliveryClientCache>();
        }
        private static IServiceCollection RegisterDependencies(this IServiceCollection services, CacheTypeEnum cacheType)
        {
            switch (cacheType)
            {
                case CacheTypeEnum.Memory:
                    services.TryAddSingleton<IDeliveryCacheManager, MemoryCacheManager>();
                    services.TryAddSingleton<IMemoryCache, MemoryCache>();
                    break;

                case CacheTypeEnum.Distributed:
                    services.TryAddSingleton<IDeliveryCacheManager, DistributedCacheManager>();
                    services.TryAddSingleton<IDistributedCache, MemoryDistributedCache>();
                    break;
            }

            return services;
        }
    }
}
