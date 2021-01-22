using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Caching;
using Kentico.Kontent.Delivery.Caching.Extensions;
using Kentico.Kontent.Delivery.Configuration;
using Kentico.Kontent.Delivery.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Kentico.Kontent.Delivery.Abstractions.Extensions;
using System;
using Kentico.Kontent.Delivery.Extensions.DependencyInjection.CustomServiceProviders;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection.Extensions
{
    /// <summary>
    /// A class which contains extension methods on <see cref="IServiceCollection"/> for registering an <see cref="IDeliveryClient"/> instance.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a delegate that will be used to configure a named <see cref="IDeliveryClient"/> via <see cref="IDeliveryClientFactory"/>
        /// </summary>
        ///<param name="name">The name of the client configuration</param>
        /// <param name="services">A <see cref="ServiceCollection"/> instance for registering and resolving dependencies.</param>
        /// <param name="buildDeliveryOptions">A function that is provided with an instance of <see cref="DeliveryOptionsBuilder"/>and expected to return a valid instance of <see cref="DeliveryOptions"/>.</param>
        /// <param name="customServiceProviderType">A custom service provider type.</param>
        /// <returns>The <paramref name="services"/> instance with <see cref="IDeliveryClient"/> registered in it</returns>
        public static IServiceCollection AddDeliveryClient(this IServiceCollection services, string name, Func<IDeliveryOptionsBuilder, DeliveryOptions> buildDeliveryOptions, CustomServiceProviderType customServiceProviderType = CustomServiceProviderType.None)
        {
            if (buildDeliveryOptions == null)
            {
                throw new ArgumentNullException(nameof(buildDeliveryOptions), "The function for creating Delivery options is null.");
            }

            var options = DeliveryOptionsHelpers.Build(buildDeliveryOptions);
            options.Name = name;

            return services
                .RegisterOptions(options, name)
                .RegisterDependencies(true)
                .RegisterNamedServices(customServiceProviderType);
        }

        /// <summary>
        /// Registers a delegate that will be used to configure a named <see cref="IDeliveryClient"/> via <see cref="IDeliveryClientFactory"/>
        /// </summary>
        ///<param name="name">The name of the client configuration</param>
        /// <param name="services">A <see cref="ServiceCollection"/> instance for registering and resolving dependencies.</param>
        /// <param name="deliveryOptions">A <see cref="DeliveryOptions"/> instance.  Options themselves are not further validated (see <see cref="DeliveryOptionsValidator.Validate"/>).</param>
        /// <param name="customServiceProviderType">A custom service provider type.</param>
        /// <returns>The <paramref name="services"/> instance with <see cref="IDeliveryClient"/> registered in it</returns>
        public static IServiceCollection AddDeliveryClient(this IServiceCollection services, string name, DeliveryOptions deliveryOptions, CustomServiceProviderType customServiceProviderType = CustomServiceProviderType.None)
        {
            if (deliveryOptions == null)
            {
                throw new ArgumentNullException(nameof(deliveryOptions), "The Delivery options object is not specified.");
            }

            deliveryOptions.Name = name;

            return services
                .RegisterOptions(deliveryOptions, name)
                .RegisterDependencies(true)
                .RegisterNamedServices(customServiceProviderType);
        }

        /// <summary>
        /// Registers a delegate that will be used to configure a named <see cref="IDeliveryClient"/> via <see cref="IDeliveryClientFactory"/>
        /// </summary>
        /// <param name="services">A <see cref="ServiceCollection"/> instance for registering and resolving dependencies.</param>
        /// <param name="name">The name of the client configuration</param>
        /// <param name="configuration">A set of key/value application configuration properties.</param>
        /// <param name="configurationSectionName">The section name of the configuration that keeps the <see cref="DeliveryOptions"/> properties. The default value is DeliveryOptions.</param>
        /// <param name="customServiceProviderType">A custom service provider type.</param>
        /// <returns>The <paramref name="services"/> instance with <see cref="IDeliveryClient"/> registered in it</returns>
        public static IServiceCollection AddDeliveryClient(this IServiceCollection services, string name, IConfiguration configuration, string configurationSectionName = "DeliveryOptions", CustomServiceProviderType customServiceProviderType = CustomServiceProviderType.None)
        {
            var options = (DeliveryOptions)configuration.GetSection(configurationSectionName);
            options.Name = name;

            return services
                .RegisterOptions(options, name)
                .RegisterDependencies(true)
                .RegisterNamedServices(customServiceProviderType);
        }

        /// <summary>
        ///  Registers a delegate that will be used to configure a cached <see cref="IDeliveryClient"/>.
        /// </summary>
        /// <param name="services">A <see cref="IServiceCollection"/> instance for registering and resolving dependencies.</param>
        /// <param name="name">A name of named client which want to use cached. <see cref="IDeliveryClient"/></param>
        /// <param name="options">A <see cref="DeliveryCacheOptions"/> instance. </param> 
        /// <returns>The <paramref name="services"/> instance with cache services registered in it</returns>
        public static IServiceCollection AddDeliveryClientCache(this IServiceCollection services, string name, DeliveryCacheOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), "The Delivery cache  options object is not specified.");
            }

            options.Name = name;

            return services
                .Configure<DeliveryCacheOptions>(name, (o) => o.Configure(options))
                .RegisterDependencies(options.CacheType, name)
                .RegisterDeliveryClientFactoryCacheDecorator();
        }

        private static IServiceCollection RegisterDeliveryClientFactoryCacheDecorator(this IServiceCollection services)
        {
            return services.Decorate<IDeliveryClientFactory, NamedDeliveryClientCacheFactory>();
        }

        private static IServiceCollection RegisterNamedServices(this IServiceCollection services, CustomServiceProviderType customServiceProviderType)
        {
            if (customServiceProviderType == CustomServiceProviderType.Autofac)
            {
                services.TryAddSingleton<INamedServiceProvider, AutofacServiceProvider>();
            }

            services.AddSingleton<IDeliveryClientFactory, NamedDeliveryClientFactory>();

            return services;
        }

        private static IServiceCollection RegisterOptions(this IServiceCollection services, DeliveryOptions options, string name)
        {
            return services.Configure<DeliveryOptions>(name, (o) => o.Configure(options));
        }

        private static IServiceCollection RegisterDependencies(this IServiceCollection services, CacheTypeEnum cacheType, string name)
        {
            switch (cacheType)
            {
                case CacheTypeEnum.Memory:
                    services.TryAddSingleton<IMemoryCache, MemoryCache>();
                    break;

                case CacheTypeEnum.Distributed:
                    services.TryAddSingleton<IDistributedCache, MemoryDistributedCache>();
                    break;
            }

            return services;
        }
    }
}
