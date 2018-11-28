using System;
using System.Net.Http;
using KenticoCloud.Delivery.Builders.DeliveryOptions;
using KenticoCloud.Delivery.CodeFirst;
using KenticoCloud.Delivery.ContentLinks;
using KenticoCloud.Delivery.InlineContentItems;
using KenticoCloud.Delivery.ResiliencePolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// A class which contains extension methods on <see cref="IServiceCollection"/> for registering an <see cref="IDeliveryClient"/> instance.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
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

        private static IServiceCollection RegisterDependencies(this IServiceCollection services)
        {
            services.TryAddSingleton<IContentLinkUrlResolver, DefaultContentLinkUrlResolver>();
            services.TryAddSingleton<ICodeFirstTypeProvider, DefaultTypeProvider>();
            services.TryAddSingleton(new HttpClient());            
            services.TryAddSingleton<IInlineContentItemsResolver<object>, ReplaceWithWarningAboutRegistrationResolver>();
            services.TryAddSingleton<IInlineContentItemsResolver<UnretrievedContentItem>, ReplaceWithWarningAboutUnretrievedItemResolver>();            
            services.TryAddSingleton<IInlineContentItemsResolverCollection, InlineContentItemsResolverCollection>();
            services.TryAddSingleton<IInlineContentItemsProcessor, InlineContentItemsProcessor>();
            services.TryAddSingleton<ICodeFirstModelProvider, CodeFirstModelProvider>();
            services.TryAddSingleton<ICodeFirstPropertyMapper, CodeFirstPropertyMapper>();
            services.TryAddSingleton<IResiliencePolicyProvider, DefaultResiliencePolicyProvider>();
            services.TryAddSingleton<IDeliveryClient, DeliveryClient>();

            return services;
        }

        // Options here are not validated on purpose, it is left to users to validate them if they want to.
        private static IServiceCollection RegisterOptions(this IServiceCollection services, DeliveryOptions options)
        {
            services.TryAddSingleton(Options.Create(options));

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
