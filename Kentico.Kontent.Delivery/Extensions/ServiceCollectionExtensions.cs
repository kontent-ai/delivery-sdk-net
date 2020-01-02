using System;
using System.Linq;
using System.Net.Http;
using Kentico.Kontent.Delivery;
using Kentico.Kontent.Delivery.Builders.DeliveryOptions;
using Kentico.Kontent.Delivery.ContentLinks;
using Kentico.Kontent.Delivery.Factories;
using Kentico.Kontent.Delivery.InlineContentItems;
using Kentico.Kontent.Delivery.RetryPolicy;
using Kentico.Kontent.Delivery.StrongTyping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.2
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// A class which contains extension methods on <see cref="IServiceCollection"/> for registering an <see cref="IDeliveryClient"/> instance.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="IDeliveryClient"/> via <see cref="IDeliveryClientFactory"/>
        /// </summary>
        ///<param name="name">The name of the client configuration</param>
        /// <param name="services">A <see cref="ServiceCollection"/> instance for registering and resolving dependencies.</param>
        /// <param name="buildDeliveryOptions">A function that is provided with an instance of <see cref="DeliveryOptionsBuilder"/>and expected to return a valid instance of <see cref="DeliveryOptions"/>.</param>
        /// <returns>The <paramref name="services"/> instance with <see cref="IDeliveryClient"/> registered in it</returns>
        public static IServiceCollection AddDeliveryClient(this IServiceCollection services, string name, Func<IDeliveryOptionsBuilder, DeliveryOptions> buildDeliveryOptions)
        {
            if (buildDeliveryOptions == null)
            {
                throw new ArgumentNullException(nameof(buildDeliveryOptions), "The function for creating Delivery options is null.");
            }

            services.TryAddSingleton<IDeliveryClientFactory, DeliveryClientFactory>();
            services.AddTransient<IConfigureOptions<DeliveryClientFactoryOptions>>(s =>
            {
                return new ConfigureNamedOptions<DeliveryClientFactoryOptions>(name, options =>
                {
                    options.DeliveryClientActions.Add(() =>
                    {
                        var serviceCollection = new ServiceCollection();
                        serviceCollection.AddDeliveryClient(buildDeliveryOptions);
                        var serviceProvider = serviceCollection.BuildServiceProvider();
                        return serviceProvider.GetService<IDeliveryClient>();
                    });
                });

            });
            return services.RegisterFactoryDependencies();
        }

        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="IDeliveryClient"/> via <see cref="IDeliveryClientFactory"/>
        /// </summary>
        /// <param name="services">A <see cref="ServiceCollection"/> instance for registering and resolving dependencies.</param>
        /// <param name="name">The name of the client configuration</param>
        /// <param name="configureDeliveryClient">A delegate that is used to configure an <see cref="IDeliveryClient"/>.</param>
        /// <returns></returns>
        public static IServiceCollection AddDeliveryClient(this IServiceCollection services, string name, Func<IDeliveryClient> configureDeliveryClient)
        {
            if (configureDeliveryClient == null)
            {
                throw new ArgumentNullException(nameof(configureDeliveryClient), "The function for creating DeliveryClient is null.");
            }

            services.TryAddSingleton<IDeliveryClientFactory, DeliveryClientFactory>();
            services.AddTransient<IConfigureOptions<DeliveryClientFactoryOptions>>(s =>
            {
                return new ConfigureNamedOptions<DeliveryClientFactoryOptions>(name, options =>
                {
                    options.DeliveryClientActions.Add(configureDeliveryClient);
                });

            });
            return services.RegisterFactoryDependencies();
        }


        /// <summary>
        /// Adds an options that will be used to configure a named <see cref="IDeliveryClient"/> via <see cref="IDeliveryClientFactory"/>
        /// </summary>
        /// <param name="name">The name of the client configuration</param>
        /// <param name="services">A <see cref="ServiceCollection"/> instance for registering and resolving dependencies.</param>
        /// <param name="deliveryOptions">A <see cref="DeliveryOptions"/> instance.  Options themselves are not further validated (see <see cref="DeliveryOptionsValidator.Validate"/>).</param>
        /// <returns>The <paramref name="services"/> instance with <see cref="IDeliveryClient"/> registered in it</returns>
        public static IServiceCollection AddDeliveryClient(this IServiceCollection services, string name, DeliveryOptions deliveryOptions)
        {
            if (deliveryOptions == null)
            {
                throw new ArgumentNullException(nameof(deliveryOptions), "The Delivery options object is not specified.");
            }

            services.TryAddSingleton<IDeliveryClientFactory, DeliveryClientFactory>();
            services.AddTransient<IConfigureOptions<DeliveryClientFactoryOptions>>(s =>
            {
                return new ConfigureNamedOptions<DeliveryClientFactoryOptions>(name, options =>
                {
                    options.DeliveryClientActions.Add(() =>
                    {
                        var serviceCollection = new ServiceCollection();
                        serviceCollection.AddDeliveryClient(deliveryOptions);
                        var serviceProvider = serviceCollection.BuildServiceProvider();
                        return serviceProvider.GetService<IDeliveryClient>();
                    });
                });

            });
            return services.RegisterFactoryDependencies();
        }

        /// <summary>
        /// Registers a <see cref="IDeliveryClient"/> instance to an <see cref="IDeliveryClient"/> interface in <see cref="ServiceCollection"/>.
        /// </summary>
        /// <param name="services">A <see cref="ServiceCollection"/> instance for registering and resolving dependencies.</param>
        /// <param name="buildDeliveryOptions">A function that is provided with an instance of <see cref="DeliveryOptionsBuilder"/>and expected to return a valid instance of <see cref="DeliveryOptions"/>.</param>
        /// <returns>The <paramref name="services"/> instance with <see cref="IDeliveryClient"/> registered in it</returns>
        public static IServiceCollection AddDeliveryClient(this IServiceCollection services, Func<IDeliveryOptionsBuilder, DeliveryOptions> buildDeliveryOptions)
        {
            if (buildDeliveryOptions == null)
            {
                throw new ArgumentNullException(nameof(buildDeliveryOptions), "The function for creating Delivery options is null.");
            }

            return services
                .BuildOptions(buildDeliveryOptions)
                .RegisterDependencies();
        }

        /// <summary>
        /// Registers a <see cref="IDeliveryClient"/> instance to an <see cref="IDeliveryClient"/> interface in <see cref="ServiceCollection"/>.
        /// </summary>
        /// <param name="services">A <see cref="ServiceCollection"/> instance for registering and resolving dependencies.</param>
        /// <param name="deliveryOptions">A <see cref="DeliveryOptions"/> instance.  Options themselves are not further validated (see <see cref="DeliveryOptionsValidator.Validate"/>).</param>
        /// <returns>The <paramref name="services"/> instance with <see cref="IDeliveryClient"/> registered in it</returns>
        public static IServiceCollection AddDeliveryClient(this IServiceCollection services, DeliveryOptions deliveryOptions)
        {
            if (deliveryOptions == null)
            {
                throw new ArgumentNullException(nameof(deliveryOptions), "The Delivery options object is not specified.");
            }

            return services
                .RegisterOptions(deliveryOptions)
                .RegisterDependencies();
        }

        /// <summary>
        /// Registers a <see cref="IDeliveryClient"/> instance to an <see cref="IDeliveryClient"/> interface in <see cref="ServiceCollection"/>.
        /// </summary>
        /// <param name="services">A <see cref="ServiceCollection"/> instance for registering and resolving dependencies.</param>
        /// <param name="configuration">A set of key/value application configuration properties.</param>
        /// <param name="configurationSectionName">The section name of the configuration that keeps the <see cref="DeliveryOptions"/> properties. The default value is DeliveryOptions.</param>
        /// <returns>The <paramref name="services"/> instance with <see cref="IDeliveryClient"/> registered in it</returns>
        public static IServiceCollection AddDeliveryClient(this IServiceCollection services, IConfiguration configuration, string configurationSectionName = "DeliveryOptions")
            => services
                .LoadOptionsConfiguration(configuration, configurationSectionName)
                .RegisterDependencies();

        /// <summary>
        /// Registers an <see cref="IInlineContentItemsResolver{T}"/> implementation For <seealso cref="InlineContentItemsProcessor"/> in <see cref="ServiceCollection"/>.
        /// </summary>
        /// <typeparam name="TContentItem">Type of content item that <paramref name="resolver"/> works with</typeparam>
        /// <param name="services">A <see cref="ServiceCollection"/> instance for registering and resolving dependencies.</param>
        /// <param name="resolver">An <see cref="IInlineContentItemsResolver{T}"/> instance capable of resolving <typeparamref name="TContentItem"/> to a <see cref="string"/></param>
        /// <returns>The <paramref name="services"/> instance with <see cref="IDeliveryClient"/> registered in it</returns>
        public static IServiceCollection AddDeliveryInlineContentItemsResolver<TContentItem>(this IServiceCollection services, IInlineContentItemsResolver<TContentItem> resolver)
            => services
                .AddSingleton(resolver)
                .AddSingleton(TypelessInlineContentItemsResolver.Create(resolver));

        /// <summary>
        /// Registers an <see cref="IInlineContentItemsResolver{T}"/> implementation for <seealso cref="InlineContentItemsProcessor"/> in <see cref="ServiceCollection"/>.
        /// </summary>
        /// <typeparam name="TContentItem">Type of content item that <typeparamref name="TInlineContentItemsResolver"/> works with</typeparam>
        /// <typeparam name="TInlineContentItemsResolver">Type of an <see cref="IInlineContentItemsResolver{T}"/> instance capable of resolving <typeparamref name="TContentItem"/> to a <see cref="string"/></typeparam>
        /// <returns>The <paramref name="services"/> instance with <see cref="IDeliveryClient"/> registered in it</returns>
        /// <remarks>Instance of <typeparamref name="TInlineContentItemsResolver"/> is obtained through <see cref="IServiceProvider"/> thus its dependencies might be injected.</remarks>
        public static IServiceCollection AddDeliveryInlineContentItemsResolver<TContentItem, TInlineContentItemsResolver>(this IServiceCollection services)
            where TInlineContentItemsResolver : class, IInlineContentItemsResolver<TContentItem>
            => services
                .AddSingleton<IInlineContentItemsResolver<TContentItem>, TInlineContentItemsResolver>()
                .AddSingleton(provider => provider.CreateDescriptor<TContentItem>());

        private static void TryAddDeliveryInlineContentItemsResolver<TContentItem, TInlineContentItemsResolver>(this IServiceCollection services)
            where TInlineContentItemsResolver : class, IInlineContentItemsResolver<TContentItem>
        {
            if (services.Any(descriptor => descriptor.ServiceType == typeof(IInlineContentItemsResolver<TContentItem>)))
                return;

            services.AddDeliveryInlineContentItemsResolver<TContentItem, TInlineContentItemsResolver>();
        }

        private static ITypelessInlineContentItemsResolver CreateDescriptor<TContentItem>(this IServiceProvider provider)
            => TypelessInlineContentItemsResolver.Create(provider.GetService<IInlineContentItemsResolver<TContentItem>>());

        private static IServiceCollection RegisterDependencies(this IServiceCollection services)
        {
            RegisterFactoryDependencies(services);
            services.TryAddSingleton<IDeliveryClient, DeliveryClient>();
            return services;
        }

        private static IServiceCollection RegisterFactoryDependencies(this IServiceCollection services)
        {
            services.TryAddSingleton<IContentLinkUrlResolver, DefaultContentLinkUrlResolver>();
            services.TryAddSingleton<ITypeProvider, TypeProvider>();
            services.TryAddSingleton(new HttpClient());
            services.TryAddDeliveryInlineContentItemsResolver<object, ReplaceWithWarningAboutRegistrationResolver>();
            services.TryAddDeliveryInlineContentItemsResolver<UnretrievedContentItem, ReplaceWithWarningAboutUnretrievedItemResolver>();
            services.TryAddDeliveryInlineContentItemsResolver<UnknownContentItem, ReplaceWithWarningAboutUnknownItemResolver>();
            services.TryAddSingleton<IInlineContentItemsProcessor, InlineContentItemsProcessor>();
            services.TryAddSingleton<IModelProvider, ModelProvider>();
            services.TryAddSingleton<IPropertyMapper, PropertyMapper>();
            services.TryAddSingleton<IRetryPolicyProvider, DefaultRetryPolicyProvider>();

            return services;
        }


        // Options here are not validated on purpose, it is left to users to validate them if they want to.
        private static IServiceCollection RegisterOptions(this IServiceCollection services, DeliveryOptions options)
        {
            services.TryAddSingleton(Options.Options.Create(options));

            return services;
        }

        private static IServiceCollection LoadOptionsConfiguration(this IServiceCollection services, IConfiguration configuration, string configurationSectionName)
            => services
                .Configure<DeliveryOptions>(configurationSectionName == null
                    ? configuration
                    : configuration.GetSection(configurationSectionName));

        private static IServiceCollection BuildOptions(this IServiceCollection services, Func<IDeliveryOptionsBuilder, DeliveryOptions> buildDeliveryOptions)
        {
            var builder = DeliveryOptionsBuilder.CreateInstance();
            var options = buildDeliveryOptions(builder);

            return services.RegisterOptions(options);
        }
    }
}
