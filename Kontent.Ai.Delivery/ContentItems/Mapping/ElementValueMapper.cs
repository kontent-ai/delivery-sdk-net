using System.Text.Json;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.ContentItems.Elements;
using Kontent.Ai.Delivery.ContentItems.Processing;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.ContentItems.Mapping;

internal sealed class ElementValueMapper(
    IOptionsMonitor<DeliveryOptions> deliveryOptions,
    IContentDependencyExtractor dependencyExtractor,
    JsonSerializerOptions jsonOptions,
    IHtmlParser htmlParser,
    ILogger<ElementValueMapper>? logger = null)
{
    private readonly IOptionsMonitor<DeliveryOptions> _deliveryOptions = deliveryOptions ?? throw new ArgumentNullException(nameof(deliveryOptions));
    private readonly IContentDependencyExtractor _dependencyExtractor = dependencyExtractor ?? throw new ArgumentNullException(nameof(dependencyExtractor));
    private readonly JsonSerializerOptions _jsonOptions = jsonOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
    private readonly RichTextParser _richTextParser = new RichTextParser(
            htmlParser ?? throw new ArgumentNullException(nameof(htmlParser)),
            dependencyExtractor,
            logger);
    private readonly ILogger<ElementValueMapper>? _logger = logger;

    public async Task<object?> MapElementAsync(
        PropertyMappingInfo prop,
        JsonElement envelope,
        Func<string, Task<object?>> getLinkedItem,
        MappingContext context)
    {
        ArgumentNullException.ThrowIfNull(prop);
        ArgumentNullException.ThrowIfNull(getLinkedItem);
        ArgumentNullException.ThrowIfNull(context);

        switch (prop.MapKind)
        {
            case ElementMappingKind.RichText:
                return await MapRichTextAsync(prop.ElementCodename, envelope, getLinkedItem, context.DependencyContext)
                    .ConfigureAwait(false);
            case ElementMappingKind.Assets:
                return MapAssets(envelope, context.DependencyContext);
            case ElementMappingKind.Taxonomy:
                return MapTaxonomy(envelope, context.DependencyContext);
            case ElementMappingKind.DateTime:
                return MapDateTime(envelope);
            case ElementMappingKind.LinkedItems:
                return await MapLinkedItemsAsync(envelope, getLinkedItem, context).ConfigureAwait(false);
            case ElementMappingKind.Simple:
            default:
                return MapSimpleValue(prop, envelope);
        }
    }

    private object? MapSimpleValue(PropertyMappingInfo prop, JsonElement envelope)
    {
        if (!envelope.TryGetProperty("value", out var valueElement))
        {
            return null;
        }

        if (valueElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(valueElement, prop.PropertyType, _jsonOptions);
        }
        catch (JsonException)
        {
            // Compatible behavior: skip on failure.
            return null;
        }
        catch (NotSupportedException)
        {
            // JsonSerializer throws NotSupportedException for unsupported type patterns.
            return null;
        }
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

        var richTextData = RichTextElementEnvelopeReader.Read(
            envelope,
            elementCodename,
            preserveEmptyModularContentEntries: true);

        return await _richTextParser.ConvertAsync(richTextData, getLinkedItem, dependencyContext).ConfigureAwait(false);
    }

    private List<Asset>? MapAssets(JsonElement envelope, DependencyTrackingContext? dependencyContext)
    {
        if (!TryGetArrayValue(envelope, out var arrayValue))
        {
            return null;
        }

        var defaultPreset = _deliveryOptions.CurrentValue.DefaultRenditionPreset;
        List<Asset> assets = [];

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

        return !TryGetArrayValue(envelope, out var arrayValue)
            ? null
            : [.. arrayValue.EnumerateArray()
            .Where(term => term.ValueKind == JsonValueKind.Object)
            .Select(CreateTaxonomyTerm)];
    }

    private static DateTimeContent? MapDateTime(JsonElement envelope)
    {
        var value = GetDateTimeProperty(envelope, "value");
        return value is null && !envelope.TryGetProperty("display_timezone", out _)
            ? null
            : new DateTimeContent
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

        // Track dependencies for cache invalidation.
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

            // ContentItem<T> implements IEmbeddedContent<T>, so we can cast directly.
            if (linked is IEmbeddedContent embeddedContent)
            {
                items.Add(embeddedContent);
            }
        }

        return items;
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
        {
            return new Dictionary<string, IAssetRendition>(StringComparer.Ordinal);
        }

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

        // Expected: "/", "{environmentId}/", "{assetId}/", "{filename}".
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
}
