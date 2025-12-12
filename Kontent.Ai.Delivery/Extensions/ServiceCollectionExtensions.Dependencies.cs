using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Processing;
using Kontent.Ai.Delivery.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kontent.Ai.Delivery.Extensions;

public static partial class ServiceCollectionExtensions
{
    private static readonly object _registryLock = new();

    private static void RegisterDependencies(IServiceCollection services)
    {
        // JSON serialization
        services.TryAddSingleton(RefitSettingsProvider.CreateDefaultJsonSerializerOptions());

        // HTTP handlers
        services.TryAddTransient<TrackingHandler>();
        services.TryAddTransient<DeliveryAuthenticationHandler>();

        // Core services
        services.TryAddSingleton<IPropertyMapper, PropertyMapper>();
        services.TryAddSingleton<ITypeProvider, TypeProvider>();
        services.TryAddSingleton<IItemTypingStrategy, DefaultItemTypingStrategy>();
        services.TryAddSingleton<IContentDeserializer, ContentDeserializer>();
        services.TryAddSingleton<IElementsPostProcessor, ElementsPostProcessor>();
        services.TryAddSingleton<IHtmlParser, HtmlParser>();

        // Dependency extraction - default to no-op when caching is disabled
        services.TryAddSingleton<IContentDependencyExtractor>(NullContentDependencyExtractor.Instance);
    }

    /// <summary>
    /// Gets or creates the singleton DeliveryClientRegistry instance.
    /// Thread-safe to prevent race conditions during concurrent registrations.
    /// </summary>
    private static DeliveryClientRegistry GetOrCreateRegistry(IServiceCollection services)
    {
        lock (_registryLock)
        {
            var descriptor = services.FirstOrDefault(d =>
                d.ServiceType == typeof(DeliveryClientRegistry));

            if (descriptor?.ImplementationInstance is DeliveryClientRegistry existing)
            {
                return existing;
            }

            var registry = new DeliveryClientRegistry();
            services.AddSingleton(registry);
            return registry;
        }
    }
}


