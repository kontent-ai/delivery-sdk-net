using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Abstractions.ContentItems.Processing;
using Kontent.Ai.Delivery.ContentItems.ContentLinks;
using Kontent.Ai.Delivery.ContentItems.DateTimes;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.ContentItems.Processing;

/// <summary>
/// Centralized content item hydration engine.
/// Hydrates complex element types (rich text, assets, taxonomies, date time, linked items)
/// using the captured raw element envelopes and modular content.
/// </summary>
internal sealed class HydrationEngine(
    IItemTypingStrategy typingStrategy,
    IContentDeserializer deserializer,
    IHtmlParser htmlParser,
    IOptionsMonitor<DeliveryOptions> deliveryOptions,
    IContentDependencyExtractor dependencyExtractor)
{
    private readonly IItemTypingStrategy _typingStrategy = typingStrategy ?? throw new ArgumentNullException(nameof(typingStrategy));
    private readonly IContentDeserializer _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
    private readonly IOptionsMonitor<DeliveryOptions> _deliveryOptions = deliveryOptions ?? throw new ArgumentNullException(nameof(deliveryOptions));
    private readonly IContentDependencyExtractor _dependencyExtractor = dependencyExtractor ?? throw new ArgumentNullException(nameof(dependencyExtractor));
    private readonly RichTextParser _richTextParser = new(htmlParser ?? throw new ArgumentNullException(nameof(htmlParser)), dependencyExtractor);

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _writablePropertiesCache = new();

    /// <summary>
    /// Hydrates a content item object in-place.
    /// </summary>
    internal Task HydrateAsync(
        object? contentItem,
        IReadOnlyDictionary<string, JsonElement>? modularContent,
        DependencyTrackingContext? dependencyContext,
        HydrationContext hydrationContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (contentItem is null)
        {
            return Task.CompletedTask;
        }

        // Dynamic mode is intentionally not hydrated (raw envelopes only).
        if (contentItem is IContentItem<IDynamicElements>)
        {
            return Task.CompletedTask;
        }

        if (contentItem is not IHydratableContentItem hydratable)
        {
            return Task.CompletedTask;
        }

        var elements = hydratable.ElementsObject;
        var contentType = hydratable.SystemAttributes?.Type ?? string.Empty;
        var rawElements = hydratable.RawElements;

        if (!rawElements.HasValue || rawElements.Value.ValueKind != JsonValueKind.Object)
        {
            return Task.CompletedTask;
        }

        var resolvingContext = CreateResolvingContext(modularContent, dependencyContext, hydrationContext, cancellationToken);

        return HydrateElementsAsync(elements, rawElements.Value, contentType, resolvingContext, modularContent, dependencyContext, hydrationContext, cancellationToken);
    }

    private async Task HydrateElementsAsync(
        object elements,
        JsonElement rawElements,
        string contentType,
        ResolvingContext resolvingContext,
        IReadOnlyDictionary<string, JsonElement>? modularContent,
        DependencyTrackingContext? dependencyContext,
        HydrationContext hydrationContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var elementsType = elements.GetType();
        var writableProperties = _writablePropertiesCache.GetOrAdd(
            elementsType,
            static t => [.. t.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanWrite)]);

        foreach (var property in writableProperties)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Map property to element codename via JsonPropertyName; ignore properties without a mapping
            var elementCodename = GetElementCodename(property);
            if (elementCodename is null)
            {
                continue;
            }

            if (!rawElements.TryGetProperty(elementCodename, out var elementEnvelope) || elementEnvelope.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            // Rich text
            if (typeof(IRichTextContent).IsAssignableFrom(property.PropertyType))
            {
                var richText = await HydrateRichTextAsync(elementCodename, elementEnvelope, resolvingContext, dependencyContext).ConfigureAwait(false);
                if (richText is not null)
                {
                    property.SetValue(elements, richText);
                }
                continue;
            }

            // Assets
            if (TryGetEnumerableElementType(property.PropertyType) is Type assetElementType &&
                (typeof(IAsset).IsAssignableFrom(assetElementType) || assetElementType == typeof(Asset)))
            {
                var assets = HydrateAssets(elementEnvelope, dependencyContext);
                if (assets is not null)
                {
                    property.SetValue(elements, assets);
                }
                continue;
            }

            // Taxonomy
            if (TryGetEnumerableElementType(property.PropertyType) is Type taxonomyElementType &&
                typeof(ITaxonomyTerm).IsAssignableFrom(taxonomyElementType))
            {
                var terms = HydrateTaxonomyTerms(elementEnvelope, dependencyContext);
                if (terms is not null)
                {
                    property.SetValue(elements, terms);
                }
                continue;
            }

            // DateTimeContent
            if (typeof(IDateTimeContent).IsAssignableFrom(property.PropertyType))
            {
                var dt = HydrateDateTimeContent(elementEnvelope);
                if (dt is not null)
                {
                    property.SetValue(elements, dt);
                }
                continue;
            }

            // Linked items (modular_content)
            if (TryGetEnumerableElementType(property.PropertyType) is Type linkedItemElementType &&
                typeof(IEmbeddedContent).IsAssignableFrom(linkedItemElementType))
            {
                var embedded = await HydrateLinkedItemsAsync(
                        elementEnvelope,
                        resolvingContext,
                        modularContent,
                        dependencyContext,
                        hydrationContext,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (embedded is not null)
                {
                    property.SetValue(elements, embedded);
                }
            }
        }
    }

    private async Task<IRichTextContent?> HydrateRichTextAsync(
        string elementCodename,
        JsonElement elementEnvelope,
        ResolvingContext context,
        DependencyTrackingContext? dependencyContext)
    {
        if (!elementEnvelope.TryGetProperty("value", out var valueEl) || valueEl.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var shim = new RichTextElementShim
        {
            Type = GetStringProperty(elementEnvelope, "type"),
            Name = GetStringProperty(elementEnvelope, "name"),
            Codename = elementCodename,
            Value = valueEl.GetString() ?? string.Empty,
            Images = DeserializeInlineImages(elementEnvelope),
            Links = DeserializeContentLinks(elementEnvelope),
            ModularContent = DeserializeModularContent(elementEnvelope)
        };

        return await _richTextParser.ConvertAsync(shim, context, dependencyContext).ConfigureAwait(false);
    }

    private IReadOnlyList<Asset>? HydrateAssets(JsonElement elementEnvelope, DependencyTrackingContext? dependencyContext)
    {
        if (!TryGetArrayValue(elementEnvelope, out var arrayValue))
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

            // Best-effort dependency tracking. Delivery asset objects don't include an explicit ID,
            // but the asset GUID is present as the second path segment in the asset URL:
            // https://assets.kontent.ai/{environmentId}/{assetId}/{filename}
            TrackAssetDependencyFromUrl(assetEl, dependencyContext);

            assets.Add(CreateAsset(assetEl, defaultPreset));
        }

        return assets;
    }

    private IReadOnlyList<TaxonomyTerm>? HydrateTaxonomyTerms(JsonElement elementEnvelope, DependencyTrackingContext? dependencyContext)
    {
        // Track taxonomy group for dependency tracking (no-op when dependencyContext is null)
        _dependencyExtractor.ExtractFromTaxonomyElement(elementEnvelope, dependencyContext);

        if (!TryGetArrayValue(elementEnvelope, out var arrayValue))
        {
            return null;
        }

        return [.. arrayValue.EnumerateArray()
            .Where(term => term.ValueKind == JsonValueKind.Object)
            .Select(CreateTaxonomyTerm)];
    }

    private static DateTimeContent? HydrateDateTimeContent(JsonElement elementEnvelope)
    {
        var value = GetDateTimeProperty(elementEnvelope, "value");
        if (value is null && !elementEnvelope.TryGetProperty("display_timezone", out _))
        {
            return null;
        }

        return new DateTimeContent
        {
            Value = value,
            DisplayTimezone = GetStringProperty(elementEnvelope, "display_timezone")
        };
    }

    private async Task<IReadOnlyList<IEmbeddedContent>?> HydrateLinkedItemsAsync(
        JsonElement elementEnvelope,
        ResolvingContext context,
        IReadOnlyDictionary<string, JsonElement>? modularContent,
        DependencyTrackingContext? dependencyContext,
        HydrationContext hydrationContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetArrayValue(elementEnvelope, out var arrayValue))
        {
            return null;
        }

        var codenames = arrayValue.EnumerateArray()
            .Select(el => el.GetString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (codenames.Count == 0)
        {
            return Array.Empty<IEmbeddedContent>();
        }

        // Track dependencies for cache invalidation
        if (dependencyContext is not null)
        {
            foreach (var codename in codenames)
            {
                dependencyContext.TrackItem(codename);
            }
        }

        var items = new List<IEmbeddedContent>(codenames.Count);
        foreach (var codename in codenames)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var linked = await context.GetLinkedItem(codename!).ConfigureAwait(false);
            if (linked is null)
            {
                continue;
            }

            items.Add(EmbeddedContentFactory.CreateEmbeddedContent(linked));
        }

        return items;
    }

    private ResolvingContext CreateResolvingContext(
        IReadOnlyDictionary<string, JsonElement>? modularContent,
        DependencyTrackingContext? dependencyContext,
        HydrationContext hydrationContext,
        CancellationToken cancellationToken)
    {
        return new ResolvingContext
        {
            GetLinkedItem = async codename =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (modularContent is null || !modularContent.TryGetValue(codename, out var linkedItem))
                {
                    return null!;
                }

                // Already resolved in this request
                if (hydrationContext.ResolvedItems.TryGetValue(codename, out var cached))
                {
                    return cached;
                }

                var contentType = ExtractContentType(linkedItem);
                var modelType = _typingStrategy.ResolveModelType(contentType);
                var itemJson = linkedItem.GetRawText();
                var contentItem = _deserializer.DeserializeContentItem(itemJson, modelType);

                // Cycle detected: return shallow (deserialized) item and do NOT recursively hydrate.
                if (!hydrationContext.ProcessingItems.Add(codename))
                {
                    return contentItem;
                }

                try
                {
                    // Do not hydrate dynamic mode items (raw envelopes only)
                    if (modelType != typeof(DynamicElements) && modelType != typeof(IDynamicElements))
                    {
                        await HydrateAsync(contentItem, modularContent, dependencyContext, hydrationContext, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    hydrationContext.ResolvedItems[codename] = contentItem;
                    return contentItem;
                }
                finally
                {
                    hydrationContext.ProcessingItems.Remove(codename);
                }
            }
        };
    }

    internal sealed class HydrationContext
    {
        public HashSet<string> ProcessingItems { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, object> ResolvedItems { get; } = new(StringComparer.Ordinal);
    }

    private static string? GetElementCodename(PropertyInfo property)
    {
        if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
        {
            return null;
        }

        return property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
    }

    private static Type? TryGetEnumerableElementType(Type type)
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

    private static string GetStringProperty(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) ? prop.GetString() ?? string.Empty : string.Empty;

    private static int GetIntProperty(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) && prop.TryGetInt32(out var value) ? value : 0;

    private static DateTime? GetDateTimeProperty(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetDateTime()
            : null;

    private static void TrackAssetDependencyFromUrl(JsonElement assetElement, DependencyTrackingContext? dependencyContext)
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
            return;
        }

        // Expected: "/", "{environmentId}/", "{assetId}/", "{filename}"
        if (uri.Segments.Length < 3)
        {
            return;
        }

        var assetIdSegment = uri.Segments[2].Trim('/');
        if (Guid.TryParse(assetIdSegment, out var assetId))
        {
            dependencyContext.TrackAsset(assetId);
        }
    }

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

