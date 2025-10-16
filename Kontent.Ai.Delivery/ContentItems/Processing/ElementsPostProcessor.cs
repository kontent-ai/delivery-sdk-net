using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.IO;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Options;
using Kontent.Ai.Delivery.Abstractions.ContentItems.Processing;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Post-processes deserialized content items to hydrate advanced element types
/// such as rich text blocks using original element JSON and modular content.
/// </summary>
/// <param name="propertyMapper">The property mapper.</param>
/// <param name="typingStrategy">The typing strategy.</param>
/// <param name="deserializer">The content deserializer.</param>
/// <param name="htmlParser">The HTML parser.</param>
/// <param name="deliveryOptions">The delivery options.</param>
internal sealed class ElementsPostProcessor(
    IPropertyMapper propertyMapper,
    IItemTypingStrategy typingStrategy,
    IContentDeserializer deserializer,
    IHtmlParser htmlParser,
    IOptionsMonitor<DeliveryOptions> deliveryOptions) : IElementsPostProcessor
{
    private readonly RichTextParser _richTextParser = new(htmlParser);
    private static readonly ConcurrentDictionary<string, JsonSerializerOptions> _richTextOptionsCache = new();
    /// <summary>
    /// Hydrates advanced element types on a strongly typed content item.
    /// </summary>
    public async Task ProcessAsync<TModel>(
        IContentItem<TModel> item,
        IReadOnlyDictionary<string, JsonElement>? modularContent,
        DependencyTrackingContext? dependencyContext = null,
        CancellationToken cancellationToken = default) where TModel : IElementsModel
    {
        if (item is not ContentItem<TModel> concrete || !concrete.RawElements.HasValue || concrete.RawElements.Value.ValueKind != JsonValueKind.Object)
            return;

        var elementsJson = concrete.RawElements.Value;
        var writableProperties = item.Elements.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanWrite);

        var resolvingContext = CreateResolvingContext(modularContent);

        // Process rich text properties in parallel
        await ProcessPropertiesAsync(
            writableProperties.Where(IsRichTextProperty),
            prop => ProcessRichTextPropertyAsync(prop, elementsJson, item.System.Type, resolvingContext, dependencyContext),
            (prop, value) => prop.SetValue(item.Elements, value));

        // Process asset properties in parallel
        await ProcessPropertiesAsync(
            writableProperties.Where(IsAssetProperty),
            prop => ProcessAssetPropertyAsync(prop, elementsJson, item.System.Type, dependencyContext),
            (prop, value) => prop.SetValue(item.Elements, value));

        // Process taxonomy properties in parallel
        await ProcessPropertiesAsync(
            writableProperties.Where(IsTaxonomyProperty),
            prop => ProcessTaxonomyPropertyAsync(prop, elementsJson, item.System.Type, dependencyContext),
            (prop, value) => prop.SetValue(item.Elements, value));

        // Process datetime content properties in parallel
        await ProcessPropertiesAsync(
            writableProperties.Where(IsDateTimeContentProperty),
            prop => ProcessDateTimePropertyAsync(prop, elementsJson, item.System.Type),
            (prop, value) => prop.SetValue(item.Elements, value));
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

        if (!TryGetStringValue(elementValue, out var stringValue))
            return null;

        var richElement = DeserializeRichTextElement(elementValue.GetRawText(), elementName);
        if (richElement is null)
            return null;

        return await _richTextParser.ConvertAsync(richElement, context, dependencyContext).ConfigureAwait(false);
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

        var dateTimeElement = JsonSerializer.Deserialize<Elements.DateTimeElementValue>(elementValue.GetRawText());
        if (dateTimeElement is null)
            return Task.FromResult<object?>(null);

        var result = new DateTimes.DateTimeContent
        {
            Value = dateTimeElement.Value,
            DisplayTimezone = dateTimeElement.DisplayTimezone
        };

        return Task.FromResult<object?>(result);
    }

    private (string Name, JsonElement Value)? FindElement(JsonElement elementsJson, PropertyInfo property, string contentType) =>
        elementsJson.EnumerateObject()
            .Where(prop => propertyMapper.IsMatch(property, prop.Name, contentType))
            .Select(prop => ((string, JsonElement)?)(prop.Name, prop.Value))
            .FirstOrDefault();

    private ResolvingContext CreateResolvingContext(IReadOnlyDictionary<string, JsonElement>? modularContent) =>
        new()
        {
            GetLinkedItem = codename =>
            {
                if (modularContent is null || !modularContent.TryGetValue(codename, out var linkedItem))
                    return Task.FromResult<object>(null!);

                var contentType = ExtractContentType(linkedItem);
                var modelType = typingStrategy.ResolveModelType(contentType);
                var itemJson = SerializeJsonElement(linkedItem);
                var contentItem = deserializer.DeserializeContentItem(itemJson, modelType);

                var result = contentItem.GetType().GetProperty("Elements")?.GetValue(contentItem) ?? contentItem;
                return Task.FromResult(result);
            }
        };

    // Simple property type predicates
    private static bool IsRichTextProperty(PropertyInfo property) =>
        typeof(IRichTextContent).IsAssignableFrom(property.PropertyType);

    private static bool IsAssetProperty(PropertyInfo property) =>
        GetEnumerableElementType(property.PropertyType) is Type elementType &&
        (typeof(IAsset).IsAssignableFrom(elementType) || elementType == typeof(Asset));

    private static bool IsTaxonomyProperty(PropertyInfo property) =>
        GetEnumerableElementType(property.PropertyType) is Type elementType &&
        typeof(ITaxonomyTerm).IsAssignableFrom(elementType);

    private static bool IsDateTimeContentProperty(PropertyInfo property) =>
        typeof(IDateTimeContent).IsAssignableFrom(property.PropertyType);

    // Helper: Get T from IEnumerable<T>
    private static Type? GetEnumerableElementType(Type type)
    {
        var enumerableInterface = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerableInterface?.GetGenericArguments().FirstOrDefault();
    }

    // JSON extraction helpers
    private static bool TryGetStringValue(JsonElement element, out string value)
    {
        if (element.TryGetProperty("value", out var valueEl) && valueEl.ValueKind == JsonValueKind.String)
        {
            value = valueEl.GetString() ?? string.Empty;
            return true;
        }
        value = string.Empty;
        return false;
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

    private static string SerializeJsonElement(JsonElement element)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();
        foreach (var prop in element.EnumerateObject())
        {
            writer.WritePropertyName(prop.Name);
            prop.Value.WriteTo(writer);
        }
        writer.WriteEndObject();
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static Elements.RichTextElementValue? DeserializeRichTextElement(string json, string elementCodename)
    {
        var options = _richTextOptionsCache.GetOrAdd(elementCodename, codename =>
        {
            var opts = new JsonSerializerOptions();
            opts.Converters.Add(new RichTextElementValueJsonConverter(codename));
            return opts;
        });
        return JsonSerializer.Deserialize<Elements.RichTextElementValue>(json, options);
    }

    private IReadOnlyList<Asset> DeserializeAssets(
        JsonElement valueArray,
        DependencyTrackingContext? dependencyContext)
    {
        // Note: Asset tracking will be implemented in Phase 2 when asset IDs are exposed in the model.
        // For now, we accept the context parameter for API consistency but don't track assets.
        // Rich text inline images are tracked separately in RichTextParser.

        return valueArray.EnumerateArray()
            .Where(asset => asset.ValueKind == JsonValueKind.Object)
            .Select(asset => CreateAsset(asset, deliveryOptions.CurrentValue.DefaultRenditionPreset))
            .ToList();
    }

    private static IReadOnlyList<TaxonomyTerm> DeserializeTaxonomyTerms(
        JsonElement elementValue,
        DependencyTrackingContext? dependencyContext)
    {
        // Extract taxonomy group for dependency tracking
        if (dependencyContext is not null &&
            elementValue.TryGetProperty("taxonomy_group", out var taxonomyGroupEl) &&
            taxonomyGroupEl.ValueKind == JsonValueKind.String)
        {
            var taxonomyGroup = taxonomyGroupEl.GetString();
            dependencyContext.TrackTaxonomy(taxonomyGroup);
        }

        // Deserialize taxonomy terms
        if (!elementValue.TryGetProperty("value", out var valueArray) ||
            valueArray.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<TaxonomyTerm>();
        }

        return valueArray.EnumerateArray()
            .Where(term => term.ValueKind == JsonValueKind.Object)
            .Select(CreateTaxonomyTerm)
            .ToList();
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
}
