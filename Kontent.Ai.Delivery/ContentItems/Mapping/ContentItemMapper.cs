using System.Collections.Concurrent;
using System.Text.Json;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.ContentItems.ContentLinks;
using Kontent.Ai.Delivery.ContentItems.Processing;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.ContentItems.Mapping;

/// <summary>
/// Centralized content item mapper for converting JSON element envelopes to strongly-typed model properties.
/// Handles post-deserialization hydration of complex element types (rich text, assets, taxonomy, linked items).
/// </summary>
internal sealed class ContentItemMapper
{
    private readonly IItemTypingStrategy _typingStrategy;
    private readonly IContentDeserializer _deserializer;
    private readonly IOptionsMonitor<DeliveryOptions> _deliveryOptions;
    private readonly IContentDependencyExtractor _dependencyExtractor;
    private readonly RichTextParser _richTextParser;
    private readonly ILogger<ContentItemMapper>? _logger;

    private static readonly ConcurrentDictionary<Type, PropertyMappingInfo[]> _propertyCache = new();

    public ContentItemMapper(
        IItemTypingStrategy typingStrategy,
        IContentDeserializer deserializer,
        IHtmlParser htmlParser,
        IOptionsMonitor<DeliveryOptions> deliveryOptions,
        IContentDependencyExtractor dependencyExtractor,
        ILogger<ContentItemMapper>? logger = null)
    {
        _typingStrategy = typingStrategy ?? throw new ArgumentNullException(nameof(typingStrategy));
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        _deliveryOptions = deliveryOptions ?? throw new ArgumentNullException(nameof(deliveryOptions));
        _dependencyExtractor = dependencyExtractor ?? throw new ArgumentNullException(nameof(dependencyExtractor));
        _logger = logger;
        _richTextParser = new RichTextParser(
            htmlParser ?? throw new ArgumentNullException(nameof(htmlParser)),
            dependencyExtractor,
            logger);
    }

    /// <summary>
    /// Completes element hydration on a deserialized content item.
    /// Called by query builders after initial deserialization to populate
    /// complex element types (rich text, assets, taxonomy, linked items).
    /// </summary>
    /// <param name="item">The partially deserialized content item.</param>
    /// <param name="modularContent">Dictionary of linked items from API response.</param>
    /// <param name="dependencyContext">Optional context for cache dependency tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task CompleteItemAsync<TModel>(
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

        return MapElementsAsync(concrete.Elements!, concrete.RawElements!.Value, context);
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

            var value = await MapElementAsync(prop, envelope, getLinkedItem, context).ConfigureAwait(false);
            if (value is not null)
            {
                prop.SetValue(elements, value);
            }
        }
    }

    private async Task<object?> MapElementAsync(
        PropertyMappingInfo prop,
        JsonElement envelope,
        Func<string, Task<object?>> getLinkedItem,
        MappingContext context)
    {
        // Rich text
        if (typeof(IRichTextContent).IsAssignableFrom(prop.PropertyType))
        {
            return await MapRichTextAsync(prop.ElementCodename, envelope, getLinkedItem, context.DependencyContext)
                .ConfigureAwait(false);
        }

        // Assets
        if (prop.EnumerableElementType is not null &&
            (typeof(IAsset).IsAssignableFrom(prop.EnumerableElementType) ||
             prop.EnumerableElementType == typeof(Asset)))
        {
            return MapAssets(envelope, context.DependencyContext);
        }

        // Taxonomy
        if (prop.EnumerableElementType is not null &&
            typeof(ITaxonomyTerm).IsAssignableFrom(prop.EnumerableElementType))
        {
            return MapTaxonomy(envelope, context.DependencyContext);
        }

        // DateTime
        if (typeof(IDateTimeContent).IsAssignableFrom(prop.PropertyType))
        {
            return MapDateTime(envelope);
        }

        // Linked items
        if (prop.EnumerableElementType is not null &&
            typeof(IEmbeddedContent).IsAssignableFrom(prop.EnumerableElementType))
        {
            return await MapLinkedItemsAsync(envelope, getLinkedItem, context).ConfigureAwait(false);
        }

        return null;
    }

    private async Task<IRichTextContent?> MapRichTextAsync(
        string elementCodename,
        JsonElement envelope,
        Func<string, Task<object?>> getLinkedItem,
        DependencyTrackingContext? dependencyContext)
    {
        if (!envelope.TryGetProperty("value", out var valueEl) || valueEl.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var shim = new RichTextElementShim
        {
            Type = GetStringProperty(envelope, "type"),
            Name = GetStringProperty(envelope, "name"),
            Codename = elementCodename,
            Value = valueEl.GetString() ?? string.Empty,
            Images = DeserializeInlineImages(envelope),
            Links = DeserializeContentLinks(envelope),
            ModularContent = DeserializeModularContent(envelope)
        };

        return await _richTextParser.ConvertAsync(shim, getLinkedItem, dependencyContext).ConfigureAwait(false);
    }

    private List<Asset>? MapAssets(JsonElement envelope, DependencyTrackingContext? dependencyContext)
    {
        if (!TryGetArrayValue(envelope, out var arrayValue))
        {
            return null;
        }

        var defaultPreset = _deliveryOptions.CurrentValue.DefaultRenditionPreset;
        var assets = new List<Asset>();

        foreach (var assetEl in arrayValue.EnumerateArray())
        {
            if (assetEl.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            TrackAssetDependencyFromUrl(assetEl, dependencyContext);
            assets.Add(CreateAsset(assetEl, defaultPreset));
        }

        return assets;
    }

    private IReadOnlyList<TaxonomyTerm>? MapTaxonomy(JsonElement envelope, DependencyTrackingContext? dependencyContext)
    {
        _dependencyExtractor.ExtractFromTaxonomyElement(envelope, dependencyContext);

        if (!TryGetArrayValue(envelope, out var arrayValue))
        {
            return null;
        }

        return [.. arrayValue.EnumerateArray()
            .Where(term => term.ValueKind == JsonValueKind.Object)
            .Select(CreateTaxonomyTerm)];
    }

    private static DateTimeContent? MapDateTime(JsonElement envelope)
    {
        var value = GetDateTimeProperty(envelope, "value");
        if (value is null && !envelope.TryGetProperty("display_timezone", out _))
        {
            return null;
        }

        return new DateTimeContent
        {
            Value = value,
            DisplayTimezone = GetStringProperty(envelope, "display_timezone")
        };
    }

    private static async Task<IReadOnlyList<IEmbeddedContent>?> MapLinkedItemsAsync(
        JsonElement envelope,
        Func<string, Task<object?>> getLinkedItem,
        MappingContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        if (!TryGetArrayValue(envelope, out var arrayValue))
        {
            return null;
        }

        var codenames = arrayValue.EnumerateArray()
            .Select(el => el.GetString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (codenames.Count == 0)
        {
            return [];
        }

        // Track dependencies for cache invalidation
        if (context.DependencyContext is not null)
        {
            foreach (var codename in codenames)
            {
                context.DependencyContext.TrackItem(codename);
            }
        }

        var items = new List<IEmbeddedContent>(codenames.Count);
        foreach (var codename in codenames)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var linked = await getLinkedItem(codename!).ConfigureAwait(false);

            // ContentItem<T> implements IEmbeddedContent<T>, so we can cast directly
            if (linked is IEmbeddedContent embeddedContent)
            {
                items.Add(embeddedContent);
            }
        }

        return items;
    }

    private Func<string, Task<object?>> CreateLinkedItemResolver(MappingContext context)
    {
        return codename => ResolveLinkedItemAsync(codename, context);
    }

    /// <summary>
    /// Resolves a linked item by codename from modular content.
    /// Handles cycle detection, memoization, and recursive hydration.
    /// Returns null if the linked item is not found in the modular content.
    /// </summary>
    private async Task<object?> ResolveLinkedItemAsync(string codename, MappingContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        if (context.ModularContent is null ||
            !context.ModularContent.TryGetValue(codename, out var linkedItem))
        {
            if (_logger is not null)
            {
                LoggerMessages.LinkedItemNotFound(_logger, codename);
            }
            return null;
        }

        // Already resolved in this request - return cached instance
        if (context.ResolvedItems.TryGetValue(codename, out var cached))
        {
            return cached;
        }

        var contentType = ExtractContentType(linkedItem);
        var modelType = _typingStrategy.ResolveModelType(contentType);
        var itemJson = linkedItem.GetRawText();
        var contentItem = _deserializer.DeserializeContentItem(itemJson, modelType);

        // Cycle detected: return shallow (deserialized) item without recursive mapping
        if (!context.ProcessingItems.Add(codename))
        {
            if (_logger is not null)
            {
                LoggerMessages.CircularReferenceDetected(_logger, codename);
            }
            return contentItem;
        }

        try
        {
            // Skip hydration for dynamic mode items (they keep raw JSON envelopes)
            if (modelType != typeof(DynamicElements) && modelType != typeof(IDynamicElements))
            {
                if (contentItem is IRawContentItem rawContentItem &&
                    rawContentItem.RawElements.HasValue)
                {
                    await MapElementsAsync(
                        rawContentItem.Elements,
                        rawContentItem.RawElements.Value,
                        context).ConfigureAwait(false);
                }
            }

            context.ResolvedItems[codename] = contentItem;
            return contentItem;
        }
        finally
        {
            context.ProcessingItems.Remove(codename);
        }
    }

    #region JSON Helpers

    private static string ExtractContentType(JsonElement itemElement) =>
        itemElement.TryGetProperty("system", out var system) && system.TryGetProperty("type", out var type)
            ? type.GetString() ?? string.Empty
            : string.Empty;

    private static Dictionary<Guid, IInlineImage> DeserializeInlineImages(JsonElement root)
    {
        if (!root.TryGetProperty("images", out var imagesEl) || imagesEl.ValueKind != JsonValueKind.Object)
            return [];

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

    private static Dictionary<Guid, IContentLink> DeserializeContentLinks(JsonElement root)
    {
        if (!root.TryGetProperty("links", out var linksEl) || linksEl.ValueKind != JsonValueKind.Object)
            return [];

        var result = new Dictionary<Guid, IContentLink>();
        foreach (var prop in linksEl.EnumerateObject())
        {
            if (Guid.TryParse(prop.Name, out var id))
            {
                var link = JsonSerializer.Deserialize<ContentLink>(prop.Value.GetRawText());
                if (link is not null)
                {
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

    private static Asset CreateAsset(JsonElement assetElement, string? defaultPreset)
    {
        var renditions = ParseRenditions(assetElement);
        var url = GetStringProperty(assetElement, "url");

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

    private static string GetStringProperty(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) ? prop.GetString() ?? string.Empty : string.Empty;

    private static int GetIntProperty(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) && prop.TryGetInt32(out var value) ? value : 0;

    private static DateTime? GetDateTimeProperty(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetDateTime()
            : null;

    private void TrackAssetDependencyFromUrl(JsonElement assetElement, DependencyTrackingContext? dependencyContext)
    {
        if (dependencyContext is null)
        {
            return;
        }

        var url = GetStringProperty(assetElement, "url");
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            if (_logger is not null)
            {
                LoggerMessages.AssetUrlParsingFailed(_logger, url);
            }
            return;
        }

        // Expected: "/", "{environmentId}/", "{assetId}/", "{filename}"
        if (uri.Segments.Length < 3)
        {
            if (_logger is not null)
            {
                LoggerMessages.AssetUrlParsingFailed(_logger, url);
            }
            return;
        }

        var assetIdSegment = uri.Segments[2].Trim('/');
        if (Guid.TryParse(assetIdSegment, out var assetId))
        {
            dependencyContext.TrackAsset(assetId);
        }
        else if (_logger is not null)
        {
            LoggerMessages.AssetUrlParsingFailed(_logger, url);
        }
    }

    #endregion

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
