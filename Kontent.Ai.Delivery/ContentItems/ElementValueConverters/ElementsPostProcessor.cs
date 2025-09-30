using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.IO;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Options;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Post-processes deserialized content items to hydrate advanced element types
/// such as rich text blocks using original element JSON and modular content.
/// </summary>
/// <param name="propertyMapper">The property mapper.</param>
/// <param name="modelProvider">The model provider.</param>
/// <param name="htmlParser">The HTML parser.</param>
internal sealed class ElementsPostProcessor(
    IPropertyMapper propertyMapper,
    IModelProvider modelProvider,
    IHtmlParser htmlParser,
    IOptionsMonitor<DeliveryOptions> deliveryOptions) : IElementsPostProcessor
{
    private readonly IPropertyMapper _propertyMapper = propertyMapper ?? throw new ArgumentNullException(nameof(propertyMapper));
    private readonly IModelProvider _modelProvider = modelProvider ?? throw new ArgumentNullException(nameof(modelProvider));
    private readonly IHtmlParser _htmlParser = htmlParser ?? throw new ArgumentNullException(nameof(htmlParser));
    private readonly IOptionsMonitor<DeliveryOptions> _deliveryOptions = deliveryOptions ?? throw new ArgumentNullException(nameof(deliveryOptions));

    /// <summary>
    /// Hydrates advanced element types on a strongly typed content item.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="item">The content item to process.</param>
    /// <param name="modularContent">The modular content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ProcessAsync<TModel>(
        IContentItem<TModel> item,
        IReadOnlyDictionary<string, JsonElement>? modularContent,
        CancellationToken cancellationToken = default) where TModel : IElementsModel
    {
        if (item is not ContentItem<TModel> concrete || !concrete.RawElements.HasValue)
        {
            return;
        }

        var elementsJson = concrete.RawElements.Value;
        if (elementsJson.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var systemType = item.System.Type;
        var properties = item.Elements.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanWrite);


        // precompute stuff shared by all props
        var resolvingContext = CreateResolvingContext(modularContent);
        var contentConverter = new RichTextContentConverter(_htmlParser);

        // pipeline: filter → map → run → apply
        var results = await Task.WhenAll(
            properties
                .Where(p => typeof(IRichTextContent).IsAssignableFrom(p.PropertyType))
                .Select(async prop =>
                {
                    // find the matching element
                    var element = FindElement(elementsJson, prop, systemType);
                    if (element is null) return (prop, null);

                    var (elementName, elementValue) = element.Value;

                    // must have a string value
                    if (!elementValue.TryGetProperty("value", out var valueEl) ||
                        valueEl.ValueKind != JsonValueKind.String)
                        return (prop, null);

                    // map json → IRichTextElementValue using custom converter that injects codename
                    var options = new JsonSerializerOptions();
                    var converter = new Elements.RichTextElementValueConverter { ElementCodename = elementName };
                    options.Converters.Add(converter);

                    var richElement = JsonSerializer.Deserialize<Elements.RichTextElementValue>(
                        elementValue.GetRawText(), options);
                    if (richElement is null) return (prop, null);

                    // produce blocks
                    var blocks = await contentConverter
                        .GetPropertyValueAsync(prop, richElement, resolvingContext)
                        .ConfigureAwait(false);

                    return (prop, blocks);
                })
                ).ConfigureAwait(false);

        foreach (var (prop, blocks) in results)
        {
            if (blocks is IRichTextContent richText)
            {
                prop.SetValue(item.Elements, richText);
            }
        }

        // Hydrate asset elements (IEnumerable<Asset>/IEnumerable<IAsset>)
        var assetResults = await Task.WhenAll(
            properties
                .Where(IsAssetProperty)
                .Select(async prop =>
                {
                    var element = FindElement(elementsJson, prop, systemType);
                    if (element is null) return (prop, (object?)null);

                    var (_, elementValue) = element.Value;
                    if (!elementValue.TryGetProperty("value", out var valueEl) || valueEl.ValueKind != JsonValueKind.Array)
                        return (prop, (object?)null);

                    var assets = DeserializeAssets(valueEl);
                    return (prop, (object?)assets);
                })
            ).ConfigureAwait(false);

        foreach (var (prop, assetsObj) in assetResults)
        {
            if (assetsObj is not null)
            {
                prop.SetValue(item.Elements, assetsObj);
            }
        }
    }

    private (string Name, JsonElement Value)? FindElement(JsonElement elementsJson, PropertyInfo propertyInfo, string contentType)
    {
        foreach (var prop in elementsJson.EnumerateObject())
        {
            if (_propertyMapper.IsMatch(propertyInfo, prop.Name, contentType))
            {
                return (prop.Name, prop.Value);
            }
        }
        return null;
    }

    private ResolvingContext CreateResolvingContext(IReadOnlyDictionary<string, JsonElement>? modularContent)
    {
        // Prebuild a JSON object string for linked items so ModelProvider can parse it
        var linkedItemsJson = modularContent is null
            ? "{}"
            : BuildLinkedItemsJson(modularContent);

        async Task<object> GetLinkedItemAsync(string codename)
        {
            if (modularContent is not null && modularContent.TryGetValue(codename, out var linked))
            {
                // Use ModelProvider to map nested items; supply full linked-items map as JSON
                return await _modelProvider.GetContentItemModelAsync<object>(linked.GetRawText(), linkedItemsJson);
            }
            return null!;
        }

        return new ResolvingContext
        {
            GetLinkedItem = GetLinkedItemAsync,
            ContentLinkUrlResolver = new ContentLinks.DefaultContentLinkUrlResolver()
        };
    }

    private static string BuildLinkedItemsJson(IReadOnlyDictionary<string, JsonElement> modularContent)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();
        foreach (var kv in modularContent)
        {
            writer.WritePropertyName(kv.Key);
            kv.Value.WriteTo(writer);
        }
        writer.WriteEndObject();
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private bool IsAssetProperty(PropertyInfo property)
    {
        var type = property.PropertyType;
        // Must be IEnumerable<>
        var enumerableIface = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableIface is null) return false;

        var elementType = enumerableIface.GetGenericArguments()[0];
        return typeof(IAsset).IsAssignableFrom(elementType) || elementType == typeof(Asset);
    }

    private object DeserializeAssets(JsonElement valueArray)
    {
        var list = new List<Asset>();

        foreach (var assetEl in valueArray.EnumerateArray())
        {
            if (assetEl.ValueKind != JsonValueKind.Object) continue;

            string name = assetEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
            string description = assetEl.TryGetProperty("description", out var descEl) ? descEl.GetString() ?? string.Empty : string.Empty;
            string type = assetEl.TryGetProperty("type", out var typeEl) ? typeEl.GetString() ?? string.Empty : string.Empty;
            int size = assetEl.TryGetProperty("size", out var sizeEl) && sizeEl.TryGetInt32(out var sizeVal) ? sizeVal : 0;
            string url = assetEl.TryGetProperty("url", out var urlEl) ? urlEl.GetString() ?? string.Empty : string.Empty;
            int width = assetEl.TryGetProperty("width", out var widthEl) && widthEl.TryGetInt32(out var widthVal) ? widthVal : 0;
            int height = assetEl.TryGetProperty("height", out var heightEl) && heightEl.TryGetInt32(out var heightVal) ? heightVal : 0;

            var renditions = new Dictionary<string, IAssetRendition>(StringComparer.Ordinal);
            if (assetEl.TryGetProperty("renditions", out var rendsEl) && rendsEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var rprop in rendsEl.EnumerateObject())
                {
                    var r = rprop.Value;
                    var rendition = new AssetRendition
                    {
                        RenditionId = r.TryGetProperty("rendition_id", out var ridEl) ? ridEl.GetString() ?? string.Empty : string.Empty,
                        PresetId = r.TryGetProperty("preset_id", out var pidEl) ? pidEl.GetString() ?? string.Empty : string.Empty,
                        Width = r.TryGetProperty("width", out var rwEl) && rwEl.TryGetInt32(out var rw) ? rw : 0,
                        Height = r.TryGetProperty("height", out var rhEl) && rhEl.TryGetInt32(out var rh) ? rh : 0,
                        Query = r.TryGetProperty("query", out var qEl) ? qEl.GetString() ?? string.Empty : string.Empty
                    };
                    renditions[rprop.Name] = rendition;
                }
            }

            // Apply default rendition preset if configured
            var preset = _deliveryOptions.CurrentValue.DefaultRenditionPreset;
            if (!string.IsNullOrEmpty(preset) && renditions.TryGetValue(preset, out var presetRendition) && !string.IsNullOrEmpty(presetRendition.Query))
            {
                url = string.IsNullOrEmpty(url) ? url : $"{url}?{presetRendition.Query}";
            }

            list.Add(new Asset
            {
                Name = name,
                Description = description,
                Type = type,
                Size = size,
                Url = url,
                Width = width,
                Height = height,
                Renditions = new Dictionary<string, IAssetRendition>(renditions)
            });
        }

        // Prefer returning List<Asset> which is assignable to IEnumerable<Asset> and IEnumerable<IAsset>
        return list;
    }
}



