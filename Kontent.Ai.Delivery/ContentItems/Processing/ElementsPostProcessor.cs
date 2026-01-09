using System.Text.Json;
using Kontent.Ai.Delivery.Abstractions.ContentItems.Processing;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Kontent.Ai.Delivery.ContentItems.Processing;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Post-processes deserialized content items to hydrate advanced element types
/// such as rich text blocks using original element JSON and modular content.
/// Also tracks dependencies on assets, taxonomies, and linked items for caching support (tracking is no-op when caching is disabled).
/// </summary>
internal sealed class ElementsPostProcessor(
    HydrationEngine hydrationEngine,
    ContentItemMapper contentItemMapper,
    IOptionsMonitor<DeliveryOptions> deliveryOptions) : IElementsPostProcessor
{
    private readonly HydrationEngine _hydrationEngine = hydrationEngine ?? throw new ArgumentNullException(nameof(hydrationEngine));
    private readonly ContentItemMapper _contentItemMapper = contentItemMapper ?? throw new ArgumentNullException(nameof(contentItemMapper));
    private readonly IOptionsMonitor<DeliveryOptions> _deliveryOptions = deliveryOptions ?? throw new ArgumentNullException(nameof(deliveryOptions));

    /// <summary>
    /// Hydrates advanced element types on a strongly typed content item.
    /// This is the public entry point that creates a fresh HydrationContext or MappingContext.
    /// </summary>
    public Task ProcessAsync<TModel>(
        IContentItem<TModel> item,
        IReadOnlyDictionary<string, JsonElement>? modularContent,
        DependencyTrackingContext? dependencyContext = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (item is not ContentItem<TModel> concrete ||
            !concrete.RawElements.HasValue ||
            concrete.RawElements.Value.ValueKind != JsonValueKind.Object)
        {
            return Task.CompletedTask;
        }

        // Feature flag: use new mapper or legacy hydration engine
        if (_deliveryOptions.CurrentValue.UseNewMapper)
        {
            return ProcessWithNewMapperAsync(concrete, modularContent, dependencyContext, cancellationToken);
        }

        var hydrationContext = new HydrationEngine.HydrationContext();
        return _hydrationEngine.HydrateAsync(concrete, modularContent, dependencyContext, hydrationContext, cancellationToken);
    }

    private Task ProcessWithNewMapperAsync<TModel>(
        ContentItem<TModel> item,
        IReadOnlyDictionary<string, JsonElement>? modularContent,
        DependencyTrackingContext? dependencyContext,
        CancellationToken cancellationToken)
    {
        var context = new MappingContext
        {
            ModularContent = modularContent,
            DependencyContext = dependencyContext,
            CancellationToken = cancellationToken
        };

        return _contentItemMapper.MapElementsAsync(item.Elements!, item.RawElements!.Value, context);
    }
}
