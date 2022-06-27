using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Builders.DeliveryClient;
using Kentico.Kontent.Delivery.Builders.DeliveryClientFactory;
using Kentico.Kontent.Delivery.Caching;
using Kentico.Kontent.Delivery.Caching.Extensions;
using Kentico.Kontent.Delivery.Configuration;
using Kentico.Kontent.Delivery.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Kentico.Kontent.Delivery.Extensions.DependencyInjection
{
    /// <summary>
    /// A class which contains extension methods on <see cref="IServiceCollection"/> for registering an <see cref="IDeliveryClient"/> instance.
    /// </summary>
    public static class ServiceCollectionExtensions
    {

        /// <summary>
        /// Registers a delegate that will be used to configure a named <see cref="IDeliveryClient"/> via <see cref="IDeliveryClientFactory"/>
        /// </summary>
        /// <param name="services">A <see cref="ServiceCollection"/> instance for registering and resolving dependencies.</param>
        /// <param name="buildDeliveryClientFactory">A function that is provided with an instance of <see cref="IDeliveryClientFactoryBuilder"/>and expected to return a valid instance of <see cref="IDeliveryClientFactory"/>.</param>
        /// <returns>The <paramref name="services"/> instance with <see cref="IDeliveryClientFactory"/> registered in it</returns>
        public static IServiceCollection AddDeliveryClientDictionaryFactory(this IServiceCollection services, Func<IDeliveryClientFactoryBuilder, IDeliveryClientFactory> buildDeliveryClientFactory)
        {
            if (buildDeliveryClientFactory == null)
            {
                throw new ArgumentNullException(nameof(buildDeliveryClientFactory), "The function for creating Delivery client factory is null.");
            }

            var factory = buildDeliveryClientFactory(new DeliveryClientDictionaryFactoryBuilder());

            // TODO 312 - TyAdd or Add with exception?
            services.TryAddSingleton<IDeliveryClientFactory>(factory);

            return services;
        }

        /// <summary>
        /// Registers a delegate that will be used to configure a named <see cref="IDeliveryClient"/> via <see cref="IDeliveryClientFactory"/>
        /// </summary>
        ///<param name="name">The name of the client configuration</param>
        /// <param name="services">A <see cref="ServiceCollection"/> instance for registering and resolving dependencies.</param>
        /// <param name="buildDeliveryOptions">A function that is provided with an instance of <see cref="DeliveryOptionsBuilder"/>and expected to return a valid instance of <see cref="DeliveryOptions"/>.</param>
        /// <param name="namedServiceProviderType">A named service provider type.</param>
        /// <returns>The <paramref name="services"/> instance with <see cref="IDeliveryClient"/> registered in it</returns>
        [Obsolete("#312 Use AddDeliveryClientFactory for multiple clients.")]
        public static IServiceCollection AddDeliveryClient(this IServiceCollection services, string name, Func<IDeliveryOptionsBuilder, DeliveryOptions> buildDeliveryOptions, NamedServiceProviderType namedServiceProviderType = NamedServiceProviderType.None)
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
                .RegisterNamedServices(namedServiceProviderType);
        }

        /// <summary>
        /// Registers a delegate that will be used to configure a named <see cref="IDeliveryClient"/> via <see cref="IDeliveryClientFactory"/>
        /// </summary>
        ///<param name="name">The name of the client configuration</param>
        /// <param name="services">A <see cref="ServiceCollection"/> instance for registering and resolving dependencies.</param>
        /// <param name="deliveryOptions">A <see cref="DeliveryOptions"/> instance.  Options themselves are not further validated (see <see cref="DeliveryOptionsValidator.Validate"/>).</param>
        /// <param name="namedServiceProviderType">A named service provider type.</param>
        /// <returns>The <paramref name="services"/> instance with <see cref="IDeliveryClient"/> registered in it</returns>
        [Obsolete("#312 Use AddDeliveryClientFactory for multiple clients.")]
        public static IServiceCollection AddDeliveryClient(this IServiceCollection services, string name, DeliveryOptions deliveryOptions, NamedServiceProviderType namedServiceProviderType = NamedServiceProviderType.None)
        {
            if (deliveryOptions == null)
            {
                throw new ArgumentNullException(nameof(deliveryOptions), "The Delivery options object is not specified.");
            }

            deliveryOptions.Name = name;

            return services
                .RegisterOptions(deliveryOptions, name)
                .RegisterDependencies(true)
                .RegisterNamedServices(namedServiceProviderType);
        }

        /// <summary>
        /// Registers a delegate that will be used to configure a named <see cref="IDeliveryClient"/> via <see cref="IDeliveryClientFactory"/>
        /// </summary>
        /// <param name="services">A <see cref="ServiceCollection"/> instance for registering and resolving dependencies.</param>
        /// <param name="name">The name of the client configuration</param>
        /// <param name="configuration">A set of key/value application configuration properties.</param>
        /// <param name="configurationSectionName">The section name of the configuration that keeps the <see cref="DeliveryOptions"/> properties. The default value is DeliveryOptions.</param>
        /// <param name="namedServiceProviderType">A named service provider type.</param>
        /// <returns>The <paramref name="services"/> instance with <see cref="IDeliveryClient"/> registered in it</returns>
        [Obsolete("#312 Use AddDeliveryClientFactory for multiple clients.")]
        public static IServiceCollection AddDeliveryClient(this IServiceCollection services, string name, IConfiguration configuration, string configurationSectionName = "DeliveryOptions", NamedServiceProviderType namedServiceProviderType = NamedServiceProviderType.None)
        {
            var options = new DeliveryOptions();
            configuration.GetSection(configurationSectionName).Bind(options);
            options.Name = name;

            return services
                .RegisterOptions(options, name)
                .RegisterDependencies(true)
                .RegisterNamedServices(namedServiceProviderType);
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

        [Obsolete("#312")]
        private static IServiceCollection RegisterNamedServices(this IServiceCollection services, NamedServiceProviderType namedServiceProviderType)
        {
            if (namedServiceProviderType == NamedServiceProviderType.Autofac)
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