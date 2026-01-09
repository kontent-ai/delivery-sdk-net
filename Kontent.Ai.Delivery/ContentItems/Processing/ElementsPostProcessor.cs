using System.Text.Json;
using Kontent.Ai.Delivery.Abstractions.ContentItems.Processing;
using Kontent.Ai.Delivery.ContentItems.Mapping;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Post-processes deserialized content items to hydrate advanced element types
/// such as rich text blocks using original element JSON and modular content.
/// Also tracks dependencies on assets, taxonomies, and linked items for caching support (tracking is no-op when caching is disabled).
/// </summary>
internal sealed class ElementsPostProcessor(ContentItemMapper contentItemMapper) : IElementsPostProcessor
{
    private readonly ContentItemMapper _contentItemMapper = contentItemMapper ?? throw new ArgumentNullException(nameof(contentItemMapper));

    /// <summary>
    /// Hydrates advanced element types on a strongly typed content item.
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

        var context = new MappingContext
        {
            ModularContent = modularContent,
            DependencyContext = dependencyContext,
            CancellationToken = cancellationToken
        };

        return _contentItemMapper.MapElementsAsync(concrete.Elements!, concrete.RawElements!.Value, context);
    }
}
