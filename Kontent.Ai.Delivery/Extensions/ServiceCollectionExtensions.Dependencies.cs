using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Kontent.Ai.Delivery.ContentItems.Processing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kontent.Ai.Delivery;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterDependencies(IServiceCollection services)
    {
        // JSON serialization
        services.TryAddSingleton(RefitSettingsProvider.CreateDefaultJsonSerializerOptions());

        // Core services
        services.TryAddSingleton<ITypeProvider, TypeProvider>();
        services.TryAddSingleton<IItemTypingStrategy, DefaultItemTypingStrategy>();
        services.TryAddSingleton<IContentDeserializer, ContentDeserializer>();
        services.TryAddSingleton<ElementValueMapper>();
        services.TryAddSingleton<LinkedItemResolver>();
        services.TryAddSingleton<ContentItemMapper>();
        services.TryAddSingleton<IHtmlParser, HtmlParser>();

        // Dependency extraction is used for cache invalidation and for optional dependency metadata on results.
        services.TryAddSingleton<IContentDependencyExtractor, ContentDependencyExtractor>();
    }
}
