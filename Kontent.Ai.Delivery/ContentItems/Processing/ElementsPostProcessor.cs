using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Options;
using Kontent.Ai.Delivery.Abstractions.ContentItems.Processing;
using Kontent.Ai.Delivery.ContentItems.ContentLinks;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;
using Kontent.Ai.Delivery.ContentItems.Processing;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Post-processes deserialized content items to hydrate advanced element types
/// such as rich text blocks using original element JSON and modular content.
/// Also tracks dependencies on assets, taxonomies, and linked items for caching support (tracking is no-op when caching is disabled).
/// </summary>
/// <param name="propertyMapper">The property mapper.</param>
/// <param name="typingStrategy">The typing strategy.</param>
/// <param name="deserializer">The content deserializer.</param>
/// <param name="htmlParser">The HTML parser.</param>
/// <param name="deliveryOptions">The delivery options.</param>
/// <param name="dependencyExtractor">The dependency extractor for caching support.</param>
internal sealed class ElementsPostProcessor(
    IPropertyMapper propertyMapper,
    IItemTypingStrategy typingStrategy,
    IContentDeserializer deserializer,
    IHtmlParser htmlParser,
    IOptionsMonitor<DeliveryOptions> deliveryOptions,
    IContentDependencyExtractor dependencyExtractor) : IElementsPostProcessor
{
    private readonly RichTextParser _richTextParser = new(htmlParser, dependencyExtractor);
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _writablePropertiesCache = new();

    // Cached predicate delegates to avoid repeated allocations
    private static readonly Func<PropertyInfo, bool> _isRichTextProperty = static property =>
        typeof(IRichTextContent).IsAssignableFrom(property.PropertyType);

    private static readonly Func<PropertyInfo, bool> _isAssetProperty = static property =>
        GetEnumerableElementType(property.PropertyType) is Type elementType &&
        (typeof(IAsset).IsAssignableFrom(elementType) || elementType == typeof(Asset));

    private static readonly Func<PropertyInfo, bool> _isTaxonomyProperty = static property =>
        GetEnumerableElementType(property.PropertyType) is Type elementType &&
        typeof(ITaxonomyTerm).IsAssignableFrom(elementType);

    private static readonly Func<PropertyInfo, bool> _isDateTimeContentProperty = static property =>
        typeof(IDateTimeContent).IsAssignableFrom(property.PropertyType);

    private static readonly Func<PropertyInfo, bool> _isLinkedItemsProperty = static property =>
        GetEnumerableElementType(property.PropertyType) is Type elementType &&
        typeof(IEmbeddedContent).IsAssignableFrom(elementType);
    /// <summary>
    /// Hydrates advanced element types on a strongly typed content item.
    /// This is the public entry point that creates a fresh HydrationContext.
    /// </summary>
    public Task ProcessAsync<TModel>(
        IContentItem<TModel> item,
        IReadOnlyDictionary<string, JsonElement>? modularContent,
        DependencyTrackingContext? dependencyContext = null,
        CancellationToken cancellationToken = default) where TModel : IElementsModel
    {
        // Create a fresh hydration context for this processing request
        var hydrationContext = new HydrationContext();
        return ProcessAsyncInternal(item, modularContent, dependencyContext, hydrationContext, cancellationToken);
    }

    /// <summary>
    /// Internal implementation that accepts HydrationContext for cycle detection and caching.
    /// Called by public ProcessAsync and recursively via reflection for nested items.
    /// </summary>
    private async Task ProcessAsyncInternal<TModel>(
        IContentItem<TModel> item,
        IReadOnlyDictionary<string, JsonElement>? modularContent,
        DependencyTrackingContext? dependencyContext,
        HydrationContext hydrationContext,
        CancellationToken cancellationToken) where TModel : IElementsModel
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (item is not ContentItem<TModel> concrete || !concrete.RawElements.HasValue || concrete.RawElements.Value.ValueKind != JsonValueKind.Object)
            return;

        var elementsJson = concrete.RawElements.Value;
        var elementsType = item.Elements.GetType();
        var writableProperties = _writablePropertiesCache.GetOrAdd(
            elementsType,
            static t => [.. t.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanWrite)]);

        var resolvingContext = CreateResolvingContext(modularContent, dependencyContext, hydrationContext, cancellationToken);

        // Group properties by target processing once to avoid repeated filtering
        var richTextProps = writableProperties.Where(_isRichTextProperty).ToArray();
        var assetProps = writableProperties.Where(_isAssetProperty).ToArray();
        var taxonomyProps = writableProperties.Where(_isTaxonomyProperty).ToArray();
        var dateTimeProps = writableProperties.Where(_isDateTimeContentProperty).ToArray();
        var linkedItemsProps = writableProperties.Where(_isLinkedItemsProperty).ToArray();

        if (richTextProps.Length > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessRichTextGroupAsync(richTextProps, elementsJson, item.System.Type, resolvingContext, dependencyContext, item.Elements).ConfigureAwait(false);
        }

        if (assetProps.Length > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessAssetGroupAsync(assetProps, elementsJson, item.System.Type, dependencyContext, item.Elements).ConfigureAwait(false);
        }

        if (taxonomyProps.Length > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessTaxonomyGroupAsync(taxonomyProps, elementsJson, item.System.Type, dependencyContext, item.Elements).ConfigureAwait(false);
        }

        if (dateTimeProps.Length > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessDateTimeGroupAsync(dateTimeProps, elementsJson, item.System.Type, item.Elements).ConfigureAwait(false);
        }

        if (linkedItemsProps.Length > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessLinkedItemsGroupAsync(linkedItemsProps, elementsJson, item.System.Type, resolvingContext, dependencyContext, item.Elements).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Generic parallel property processor with filter, transform, and apply phases.
    /// </summary>
    private static async Task ProcessPropertiesAsync<T>(
        IEnumerable<PropertyInfo> properties,
        Func<PropertyInfo, Task<T?>> transform,
        Action<PropertyInfo, T> apply) where T : class
    {
        var results = await Task.WhenAll(
            properties.Select(async prop => (Property: prop, Value: await transform(prop).ConfigureAwait(false)))
        ).ConfigureAwait(false);

        foreach (var (property, value) in results.Where(r => r.Value is not null))
            apply(property, value!);
    }

    private async Task<IRichTextContent?> ProcessRichTextPropertyAsync(
        PropertyInfo property,
        JsonElement elementsJson,
        string contentType,
        ResolvingContext context,
        DependencyTrackingContext? dependencyContext)
    {
        var element = FindElement(elementsJson, property, contentType);
        if (!element.HasValue)
            return null;

        var (elementName, elementValue) = element.Value;

        if (!elementValue.TryGetProperty("value", out var valueEl) || valueEl.ValueKind != JsonValueKind.String)
            return null;

        var images = DeserializeInlineImages(elementValue);
        var links = DeserializeContentLinks(elementValue);
        var modular = DeserializeModularContent(elementValue);

        var shim = new RichTextElementShim
        {
            Type = GetStringProperty(elementValue, "type"),
            Name = GetStringProperty(elementValue, "name"),
            Codename = elementName,
            Value = valueEl.GetString() ?? string.Empty,
            Images = images,
            Links = links,
            ModularContent = modular
        };

        return await _richTextParser.ConvertAsync(shim, context, dependencyContext).ConfigureAwait(false);
    }

    private Task<object?> ProcessAssetPropertyAsync(
        PropertyInfo property,
        JsonElement elementsJson,
        string contentType,
        DependencyTrackingContext? dependencyContext)
    {
        var element = FindElement(elementsJson, property, contentType);
        if (!element.HasValue)
            return Task.FromResult<object?>(null);

        var (_, elementValue) = element.Value;

        object? result = TryGetArrayValue(elementValue, out var arrayValue)
            ? DeserializeAssets(arrayValue, dependencyContext)
            : null;

        return Task.FromResult(result);
    }

    private Task<object?> ProcessTaxonomyPropertyAsync(
        PropertyInfo property,
        JsonElement elementsJson,
        string contentType,
        DependencyTrackingContext? dependencyContext)
    {
        var element = FindElement(elementsJson, property, contentType);
        if (!element.HasValue)
            return Task.FromResult<object?>(null);

        var (_, elementValue) = element.Value;

        object? result = TryGetArrayValue(elementValue, out var arrayValue)
            ? DeserializeTaxonomyTerms(elementValue, dependencyContext)
            : null;

        return Task.FromResult(result);
    }

    private Task<object?> ProcessDateTimePropertyAsync(PropertyInfo property, JsonElement elementsJson, string contentType)
    {
        var element = FindElement(elementsJson, property, contentType);
        if (!element.HasValue)
            return Task.FromResult<object?>(null);

        var (_, elementValue) = element.Value;

        var value = GetDateTimeProperty(elementValue, "value");
        var timezone = GetStringProperty(elementValue, "display_timezone");

        var result = new DateTimes.DateTimeContent
        {
            Value = value,
            DisplayTimezone = timezone
        };

        return Task.FromResult<object?>(result);
    }

    private async Task<object?> ProcessLinkedItemsPropertyAsync(
        PropertyInfo property,
        JsonElement elementsJson,
        string contentType,
        ResolvingContext context,
        DependencyTrackingContext? dependencyContext)
    {
        var element = FindElement(elementsJson, property, contentType);
        if (!element.HasValue)
            return null;

        var (_, elementValue) = element.Value;
        if (!TryGetArrayValue(elementValue, out var arrayValue))
            return null;

        // Extract codenames from the value array
        var codenames = arrayValue.EnumerateArray()
            .Select(el => el.GetString())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        if (codenames.Count == 0)
            return Array.Empty<IEmbeddedContent>();

        // Track dependencies for cache invalidation
        if (dependencyContext is not null)
        {
            foreach (var codename in codenames)
            {
                dependencyContext.TrackItem(codename);
            }
        }

        // Hydrate items in parallel using same infrastructure as rich text
        var itemTasks = codenames.Select(async codename =>
        {
            var contentItem = await context.GetLinkedItem(codename!);
            if (contentItem is null) return null;

            return EmbeddedContentFactory.CreateEmbeddedContent(contentItem);
        });

        var items = await Task.WhenAll(itemTasks).ConfigureAwait(false);

        // Filter out nulls (unresolved items)
        return items.Where(i => i is not null).ToList();
    }

    private (string Name, JsonElement Value)? FindElement(JsonElement elementsJson, PropertyInfo property, string contentType) =>
        elementsJson.EnumerateObject()
            .Where(prop => propertyMapper.IsMatch(property, prop.Name, contentType))
            .Select(prop => ((string, JsonElement)?)(prop.Name, prop.Value))
            .FirstOrDefault();

    private ResolvingContext CreateResolvingContext(
        IReadOnlyDictionary<string, JsonElement>? modularContent,
        DependencyTrackingContext? dependencyContext,
        HydrationContext? hydrationContext = null,
        CancellationToken cancellationToken = default)
    {
        // Create or reuse hydration context for cycle detection and caching
        hydrationContext ??= new HydrationContext();

        return new()
        {
            GetLinkedItem = async codename =>
            {
                if (modularContent is null || !modularContent.TryGetValue(codename, out var linkedItem))
                    return null!;

                // Check if already fully resolved - return cached instance
                if (hydrationContext.ResolvedItems.TryGetValue(codename, out var cachedItem))
                    return cachedItem;

                var contentType = ExtractContentType(linkedItem);
                var modelType = typingStrategy.ResolveModelType(contentType);
                var itemJson = linkedItem.GetRawText();
                var contentItem = deserializer.DeserializeContentItem(itemJson, modelType);

                // Cycle detected - return shallow item (deserialized but not recursively hydrated)
                // This breaks the infinite loop while preserving the reference
                if (!hydrationContext.ProcessingItems.Add(codename))
                {
                    return contentItem;
                }

                try
                {
                    // Recursively process nested items to hydrate their complex elements
                    // (rich text, assets, taxonomy, other linked items)
                    await ProcessContentItemAsync(contentItem, modularContent, dependencyContext, hydrationContext, cancellationToken).ConfigureAwait(false);

                    // Cache the fully resolved item for reuse
                    hydrationContext.ResolvedItems[codename] = contentItem;

                    return contentItem;
                }
                finally
                {
                    // Remove from processing set after completion (allows re-entry from different paths)
                    hydrationContext.ProcessingItems.Remove(codename);
                }
            }
        };
    }

    /// <summary>
    /// Internal context for tracking hydration state to prevent cycles and cache resolved items.
    /// </summary>
    private sealed class HydrationContext
    {
        /// <summary>
        /// Items currently being processed (for cycle detection).
        /// </summary>
        public HashSet<string> ProcessingItems { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Items that have been fully resolved (for caching/reuse within same request).
        /// </summary>
        public Dictionary<string, object> ResolvedItems { get; } = new(StringComparer.Ordinal);
    }

    // Group processors
    private Task ProcessRichTextGroupAsync(
        IReadOnlyList<PropertyInfo> properties,
        JsonElement elementsJson,
        string contentType,
        ResolvingContext context,
        DependencyTrackingContext? dependencyContext,
        object target)
        => ProcessPropertiesAsync(
            properties,
            prop => ProcessRichTextPropertyAsync(prop, elementsJson, contentType, context, dependencyContext),
            (prop, value) => prop.SetValue(target, value));

    private Task ProcessAssetGroupAsync(
        IReadOnlyList<PropertyInfo> properties,
        JsonElement elementsJson,
        string contentType,
        DependencyTrackingContext? dependencyContext,
        object target)
        => ProcessPropertiesAsync(
            properties,
            prop => ProcessAssetPropertyAsync(prop, elementsJson, contentType, dependencyContext),
            (prop, value) => prop.SetValue(target, value));

    private Task ProcessTaxonomyGroupAsync(
        IReadOnlyList<PropertyInfo> properties,
        JsonElement elementsJson,
        string contentType,
        DependencyTrackingContext? dependencyContext,
        object target)
        => ProcessPropertiesAsync(
            properties,
            prop => ProcessTaxonomyPropertyAsync(prop, elementsJson, contentType, dependencyContext),
            (prop, value) => prop.SetValue(target, value));

    private Task ProcessDateTimeGroupAsync(
        IReadOnlyList<PropertyInfo> properties,
        JsonElement elementsJson,
        string contentType,
        object target)
        => ProcessPropertiesAsync(
            properties,
            prop => ProcessDateTimePropertyAsync(prop, elementsJson, contentType),
            (prop, value) => prop.SetValue(target, value));

    private Task ProcessLinkedItemsGroupAsync(
        IReadOnlyList<PropertyInfo> properties,
        JsonElement elementsJson,
        string contentType,
        ResolvingContext context,
        DependencyTrackingContext? dependencyContext,
        object target)
        => ProcessPropertiesAsync(
            properties,
            prop => ProcessLinkedItemsPropertyAsync(prop, elementsJson, contentType, context, dependencyContext),
            (prop, value) => prop.SetValue(target, value));

    // Helper: Get T from IEnumerable<T>
    private static Type? GetEnumerableElementType(Type type)
    {
        var enumerableInterface = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerableInterface?.GetGenericArguments().FirstOrDefault();
    }

    private static bool TryGetArrayValue(JsonElement element, out JsonElement arrayValue)
    {
        if (element.TryGetProperty("value", out var valueEl) && valueEl.ValueKind == JsonValueKind.Array)
        {
            arrayValue = valueEl;
            return true;
        }
        arrayValue = default;
        return false;
    }

    private static string ExtractContentType(JsonElement itemElement) =>
        itemElement.TryGetProperty("system", out var system) && system.TryGetProperty("type", out var type)
            ? type.GetString() ?? string.Empty
            : string.Empty;

    private static IDictionary<Guid, IInlineImage> DeserializeInlineImages(JsonElement root)
    {
        if (!root.TryGetProperty("images", out var imagesEl) || imagesEl.ValueKind != JsonValueKind.Object)
            return new Dictionary<Guid, IInlineImage>();

        var result = new Dictionary<Guid, IInlineImage>();
        foreach (var prop in imagesEl.EnumerateObject())
        {
            if (Guid.TryParse(prop.Name, out var id))
            {
                var image = JsonSerializer.Deserialize<InlineImage>(prop.Value.GetRawText());
                if (image is not null)
                    result[id] = image;
            }
        }
        return result;
    }

    private static IDictionary<Guid, IContentLink> DeserializeContentLinks(JsonElement root)
    {
        if (!root.TryGetProperty("links", out var linksEl) || linksEl.ValueKind != JsonValueKind.Object)
            return new Dictionary<Guid, IContentLink>();

        var result = new Dictionary<Guid, IContentLink>();
        foreach (var prop in linksEl.EnumerateObject())
        {
            if (Guid.TryParse(prop.Name, out var id))
            {
                var link = JsonSerializer.Deserialize<ContentLink>(prop.Value.GetRawText());
                if (link is not null)
                {
                    // Set the Id from the dictionary key (API returns ID as the key, not in the JSON object)
                    link.Id = id;
                    result[id] = link;
                }
            }
        }
        return result;
    }

    private static List<string> DeserializeModularContent(JsonElement root)
    {
        if (!root.TryGetProperty("modular_content", out var modularEl) || modularEl.ValueKind != JsonValueKind.Array)
            return [];
        var list = JsonSerializer.Deserialize<List<string>>(modularEl.GetRawText());
        return list ?? [];
    }

    private IReadOnlyList<Asset> DeserializeAssets(
        JsonElement valueArray,
        DependencyTrackingContext? dependencyContext)
        => [.. valueArray.EnumerateArray()
            .Where(asset => asset.ValueKind == JsonValueKind.Object)
            .Select(asset => CreateAsset(asset, deliveryOptions.CurrentValue.DefaultRenditionPreset))];

    private IReadOnlyList<TaxonomyTerm> DeserializeTaxonomyTerms(
        JsonElement elementValue,
        DependencyTrackingContext? dependencyContext)
    {
        // Extract taxonomy group for dependency tracking
        dependencyExtractor.ExtractFromTaxonomyElement(elementValue, dependencyContext);

        // Deserialize taxonomy terms
        if (!elementValue.TryGetProperty("value", out var valueArray) ||
            valueArray.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return [.. valueArray.EnumerateArray()
            .Where(term => term.ValueKind == JsonValueKind.Object)
            .Select(CreateTaxonomyTerm)];
    }

    private static Asset CreateAsset(JsonElement assetElement, string? defaultPreset)
    {
        var renditions = ParseRenditions(assetElement);
        var url = GetStringProperty(assetElement, "url");

        // Apply default rendition preset if configured
        if (!string.IsNullOrEmpty(defaultPreset) &&
            renditions.TryGetValue(defaultPreset, out var presetRendition) &&
            !string.IsNullOrEmpty(presetRendition.Query) &&
            !string.IsNullOrEmpty(url))
        {
            url = $"{url}?{presetRendition.Query}";
        }

        return new Asset
        {
            Name = GetStringProperty(assetElement, "name"),
            Description = GetStringProperty(assetElement, "description"),
            Type = GetStringProperty(assetElement, "type"),
            Size = GetIntProperty(assetElement, "size"),
            Url = url,
            Width = GetIntProperty(assetElement, "width"),
            Height = GetIntProperty(assetElement, "height"),
            Renditions = new Dictionary<string, IAssetRendition>(renditions)
        };
    }

    private static Dictionary<string, IAssetRendition> ParseRenditions(JsonElement assetElement)
    {
        if (!assetElement.TryGetProperty("renditions", out var rendsEl) || rendsEl.ValueKind != JsonValueKind.Object)
            return new Dictionary<string, IAssetRendition>(StringComparer.Ordinal);

        return rendsEl.EnumerateObject()
            .ToDictionary(
                prop => prop.Name,
                prop => (IAssetRendition)new AssetRendition
                {
                    RenditionId = GetStringProperty(prop.Value, "rendition_id"),
                    PresetId = GetStringProperty(prop.Value, "preset_id"),
                    Width = GetIntProperty(prop.Value, "width"),
                    Height = GetIntProperty(prop.Value, "height"),
                    Query = GetStringProperty(prop.Value, "query")
                },
                StringComparer.Ordinal);
    }

    private static TaxonomyTerm CreateTaxonomyTerm(JsonElement termElement) =>
        new()
        {
            Name = GetStringProperty(termElement, "name"),
            Codename = GetStringProperty(termElement, "codename")
        };

    private static string GetStringProperty(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) ? prop.GetString() ?? string.Empty : string.Empty;

    private static int GetIntProperty(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) && prop.TryGetInt32(out var value) ? value : 0;

    private static DateTime? GetDateTimeProperty(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetDateTime()
            : null;

    /// <summary>
    /// Helper method to process a dynamically-typed content item.
    /// Uses reflection to call the internal ProcessAsyncInternal with the correct generic type.
    /// </summary>
    private async Task ProcessContentItemAsync(
        object contentItem,
        IReadOnlyDictionary<string, JsonElement>? modularContent,
        DependencyTrackingContext? dependencyContext,
        HydrationContext hydrationContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (contentItem is null)
            return;

        // ContentItem implements IContentItem<TModel> where TModel : IElementsModel
        // We need to find the TModel type and call ProcessAsyncInternal<TModel>
        var contentItemType = contentItem.GetType();

        // Find IContentItem<TModel> interface
        var iContentItemInterface = contentItemType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(IContentItem<>));

        if (iContentItemInterface is null)
            return;

        var modelType = iContentItemInterface.GetGenericArguments()[0];

        // Get ProcessAsyncInternal<TModel> method (internal method that accepts HydrationContext)
        var processMethod = typeof(ElementsPostProcessor)
            .GetMethod(nameof(ProcessAsyncInternal), BindingFlags.Instance | BindingFlags.NonPublic)
            ?.MakeGenericMethod(modelType);

        if (processMethod is null)
            return;

        // Invoke ProcessAsyncInternal<TModel>(item, modularContent, dependencyContext, hydrationContext, cancellationToken)
        var task = (Task?)processMethod.Invoke(this,
        [
            contentItem,
            modularContent,
            dependencyContext,
            hydrationContext,
            cancellationToken
        ]);

        if (task is not null)
            await task.ConfigureAwait(false);
    }
    /// <summary>
    /// Shim class to adapt rich text element value for parsing.
    /// </summary>
    // TODO: explain this more thoroughly and consider simplification
    private sealed class RichTextElementShim : IRichTextElementValue
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Codename { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public IDictionary<Guid, IInlineImage> Images { get; set; } = new Dictionary<Guid, IInlineImage>();
        public IDictionary<Guid, IContentLink> Links { get; set; } = new Dictionary<Guid, IContentLink>();
        public List<string> ModularContent { get; set; } = [];
    }
}
