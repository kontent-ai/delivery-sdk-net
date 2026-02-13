using System.Collections.Concurrent;
using System.Text.Json;

namespace Kontent.Ai.Delivery.ContentItems.Mapping;

/// <summary>
/// Centralized content item mapper for converting JSON element envelopes to strongly-typed model properties.
/// Handles post-deserialization hydration of complex element types (rich text, assets, taxonomy, linked items).
/// </summary>
internal sealed class ContentItemMapper
{
    private readonly IItemTypingStrategy _typingStrategy;
    private readonly IContentDeserializer _deserializer;
    private readonly ElementValueMapper _elementValueMapper;
    private readonly LinkedItemResolver _linkedItemResolver;

    private static readonly ConcurrentDictionary<Type, PropertyMappingInfo[]> _propertyCache = new();

    public ContentItemMapper(
        IItemTypingStrategy typingStrategy,
        IContentDeserializer deserializer,
        ElementValueMapper elementValueMapper,
        LinkedItemResolver linkedItemResolver)
    {
        _typingStrategy = typingStrategy ?? throw new ArgumentNullException(nameof(typingStrategy));
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        _elementValueMapper = elementValueMapper ?? throw new ArgumentNullException(nameof(elementValueMapper));
        _linkedItemResolver = linkedItemResolver ?? throw new ArgumentNullException(nameof(linkedItemResolver));
    }

    /// <summary>
    /// Completes element hydration on a deserialized content item.
    /// Called by query builders after initial deserialization to populate
    /// complex element types (rich text, assets, taxonomy, linked items).
    /// </summary>
    /// <param name="item">The partially deserialized content item.</param>
    /// <param name="modularContent">Dictionary of linked items from API response.</param>
    /// <param name="dependencyContext">Optional context for cache dependency tracking.</param>
    /// <param name="defaultRenditionPreset">Optional default asset rendition preset codename used when mapping asset URLs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task CompleteItemAsync<TModel>(
        IContentItem<TModel> item,
        IReadOnlyDictionary<string, JsonElement>? modularContent,
        DependencyTrackingContext? dependencyContext = null,
        string? defaultRenditionPreset = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (item is not ContentItem<TModel> concrete ||
            !concrete.RawItemJson.HasValue)
        {
            return Task.CompletedTask;
        }

        // Extract elements from the full item JSON.
        if (!concrete.RawItemJson.Value.TryGetProperty("elements", out var rawElements) ||
            rawElements.ValueKind != JsonValueKind.Object)
        {
            return Task.CompletedTask;
        }

        var context = new MappingContext
        {
            ModularContent = modularContent,
            DependencyContext = dependencyContext,
            DefaultRenditionPreset = defaultRenditionPreset,
            CancellationToken = cancellationToken
        };

        // Register root item to enable circular references back to it
        // (e.g., A -> B -> A should return the same A instance).
        context.ItemsBeingHydrated[item.System.Codename] = item;

        return MapElementsAsync(concrete.Elements!, rawElements, context);
    }

    /// <summary>
    /// Attempts to resolve a content item to its runtime type based on the registered ITypeProvider.
    /// If the type provider returns a mapping for the content type, the item is deserialized and hydrated
    /// to that type. Otherwise, returns null to indicate the caller should keep the dynamic version.
    /// </summary>
    /// <param name="rawItemJson">The full JSON of the content item.</param>
    /// <param name="modularContent">Dictionary of linked items from API response.</param>
    /// <param name="dependencyContext">Optional context for cache dependency tracking.</param>
    /// <param name="defaultRenditionPreset">Optional default asset rendition preset codename used when mapping asset URLs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The runtime-typed item, or null if no type mapping exists (caller should keep dynamic version).
    /// </returns>
    public async Task<IContentItem?> TryRuntimeTypeItemAsync(
        JsonElement rawItemJson,
        IReadOnlyDictionary<string, JsonElement>? modularContent,
        DependencyTrackingContext? dependencyContext = null,
        string? defaultRenditionPreset = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var contentType = ExtractContentType(rawItemJson);
        var modelType = _typingStrategy.ResolveModelType(contentType);

        // Skip runtime typing if no mapping exists (falls back to DynamicElements).
        if (ModelTypeHelper.IsDynamic(modelType))
        {
            return null;
        }

        // Deserialize to the runtime type.
        var contentItem = _deserializer.DeserializeContentItem(rawItemJson, modelType);

        // Hydrate complex elements.
        var context = new MappingContext
        {
            ModularContent = modularContent,
            DependencyContext = dependencyContext,
            DefaultRenditionPreset = defaultRenditionPreset,
            CancellationToken = cancellationToken
        };

        await HydrateContentItemIfNeededAsync(contentItem, modelType, context).ConfigureAwait(false);

        return contentItem as IContentItem;
    }

    /// <summary>
    /// Attempts runtime type resolution for a collection of dynamic items.
    /// Items with type provider mappings are resolved to strongly-typed models;
    /// items without mappings are kept as dynamic.
    /// </summary>
    /// <param name="dynamicItems">Collection of dynamic content items.</param>
    /// <param name="modularContent">Dictionary of linked items from API response.</param>
    /// <param name="defaultRenditionPreset">Optional default asset rendition preset codename used when mapping asset URLs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of items with runtime typing applied where possible.</returns>
    internal async Task<IReadOnlyList<IContentItem>> RuntimeTypeItemsAsync(
        IReadOnlyList<IContentItem<IDynamicElements>> dynamicItems,
        IReadOnlyDictionary<string, JsonElement>? modularContent,
        string? defaultRenditionPreset = null,
        CancellationToken cancellationToken = default)
    {
        var result = new List<IContentItem>(dynamicItems.Count);

        foreach (var dynamicItem in dynamicItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (dynamicItem is IRawContentItem rawContentItem && rawContentItem.RawItemJson.HasValue)
            {
                var runtimeItem = await TryRuntimeTypeItemAsync(
                    rawContentItem.RawItemJson.Value,
                    modularContent,
                    dependencyContext: null,
                    defaultRenditionPreset,
                    cancellationToken).ConfigureAwait(false);

                if (runtimeItem != null)
                {
                    result.Add(runtimeItem);
                    continue;
                }
            }

            // Fall back to dynamic.
            result.Add(dynamicItem);
        }

        return result;
    }

    /// <summary>
    /// Maps element JSON envelopes to model properties.
    /// Used for post-deserialization completion of complex types.
    /// </summary>
    internal async Task MapElementsAsync(
        object elements,
        JsonElement rawElements,
        MappingContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        if (rawElements.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var elementsType = elements.GetType();
        var properties = _propertyCache.GetOrAdd(elementsType, PropertyMappingInfo.CreateMappings);
        var getLinkedItem = CreateLinkedItemResolver(context);

        foreach (var prop in properties)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (!rawElements.TryGetProperty(prop.ElementCodename, out var envelope) ||
                envelope.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var value = await _elementValueMapper.MapElementAsync(prop, envelope, getLinkedItem, context).ConfigureAwait(false);
            if (value is not null)
            {
                prop.SetValue(elements, value);
            }
        }
    }

    private Func<string, Task<object?>> CreateLinkedItemResolver(MappingContext context) =>
        codename => _linkedItemResolver.ResolveAsync(codename, context, HydrateContentItemIfNeededAsync);

    /// <summary>
    /// Hydrates a content item's complex elements if it's not in dynamic mode.
    /// </summary>
    private async Task HydrateContentItemIfNeededAsync(object contentItem, Type modelType, MappingContext context)
    {
        // Skip hydration for dynamic mode items (they keep raw JSON envelopes).
        if (ModelTypeHelper.IsDynamic(modelType))
        {
            return;
        }

        // Extract elements from the full item JSON.
        if (contentItem is IRawContentItem rawContentItem &&
            rawContentItem.RawItemJson.HasValue &&
            rawContentItem.Elements is not null &&
            rawContentItem.RawItemJson.Value.TryGetProperty("elements", out var rawElements) &&
            rawElements.ValueKind == JsonValueKind.Object)
        {
            await MapElementsAsync(
                rawContentItem.Elements,
                rawElements,
                context).ConfigureAwait(false);
        }
    }

    private static string ExtractContentType(JsonElement itemElement) =>
        itemElement.TryGetProperty("system", out var system) && system.TryGetProperty("type", out var type)
            ? type.GetString() ?? string.Empty
            : string.Empty;
}
