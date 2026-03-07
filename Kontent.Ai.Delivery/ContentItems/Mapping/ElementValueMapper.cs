using System.Text.Json;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.ContentItems.Elements;
using Kontent.Ai.Delivery.ContentItems.Processing;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.ContentItems.Mapping;

internal sealed class ElementValueMapper(
    IContentDependencyExtractor dependencyExtractor,
    JsonSerializerOptions jsonOptions,
    IHtmlParser htmlParser,
    ILogger<ElementValueMapper>? logger = null)
{
    private readonly RichTextParser _richTextParser = new RichTextParser(
            htmlParser ?? throw new ArgumentNullException(nameof(htmlParser)),
            dependencyExtractor,
            logger);

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
                return await MapRichTextAsync(prop.ElementCodename, envelope, getLinkedItem, context)
                    .ConfigureAwait(false);
            case ElementMappingKind.Assets:
                return MapAssets(envelope, context.DefaultRenditionPreset, context.CustomAssetDomain, context.DependencyContext);
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
            if (logger is not null)
            {
                LoggerMessages.ElementMappingSkipped(logger, prop.ElementCodename, "value");
            }
            return null;
        }

        if (valueElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(valueElement, prop.PropertyType, jsonOptions);
        }
        catch (JsonException ex)
        {
            if (logger is not null)
            {
                LoggerMessages.PropertyDeserializationFailed(
                    logger,
                    prop.ElementCodename,
                    prop.PropertyType.Name,
                    ex);
            }
            return null;
        }
        catch (NotSupportedException ex)
        {
            // JsonSerializer throws NotSupportedException for unsupported type patterns.
            if (logger is not null)
            {
                LoggerMessages.PropertyDeserializationFailed(
                    logger,
                    prop.ElementCodename,
                    prop.PropertyType.Name,
                    ex);
            }
            return null;
        }
    }

    private async Task<IRichTextContent?> MapRichTextAsync(
        string elementCodename,
        JsonElement envelope,
        Func<string, Task<object?>> getLinkedItem,
        MappingContext context)
    {
        if (!envelope.TryGetProperty("value", out var valueEl) || valueEl.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var richTextData = RichTextElementEnvelopeReader.Read(
            envelope,
            elementCodename,
            preserveEmptyModularContentEntries: true);

        if (context.CustomAssetDomain is not null && richTextData.Images.Count > 0)
        {
            var rewritten = new Dictionary<Guid, IInlineImage>(richTextData.Images.Count);
            foreach (var (id, image) in richTextData.Images)
            {
                rewritten[id] = image is InlineImage img
                    ? img with { Url = AssetUrlRewriter.RewriteUrl(img.Url, context.CustomAssetDomain) }
                    : image;
            }
            richTextData = richTextData with { Images = rewritten };
        }

        return await _richTextParser.ConvertAsync(richTextData, getLinkedItem, context.DependencyContext).ConfigureAwait(false);
    }

    private List<Asset>? MapAssets(JsonElement envelope, string? defaultRenditionPreset, Uri? customAssetDomain, DependencyTrackingContext? dependencyContext)
    {
        if (!TryGetArrayValue(envelope, out var arrayValue))
        {
            return null;
        }

        List<Asset> assets = [];

        foreach (var assetEl in arrayValue.EnumerateArray())
        {
            if (assetEl.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            TrackAssetDependencyFromUrl(assetEl, dependencyContext);
            assets.Add(CreateAsset(assetEl, defaultRenditionPreset, customAssetDomain));
        }

        return assets;
    }

    private IReadOnlyList<TaxonomyTerm>? MapTaxonomy(JsonElement envelope, DependencyTrackingContext? dependencyContext)
    {
        dependencyExtractor.ExtractFromTaxonomyElement(envelope, dependencyContext);

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

    private static Asset CreateAsset(JsonElement assetElement, string? defaultPreset, Uri? customAssetDomain)
    {
        var renditions = ParseRenditions(assetElement);
        var url = GetStringProperty(assetElement, "url");

        url = AssetUrlRewriter.RewriteUrl(url, customAssetDomain);

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
            Width = GetNullableIntProperty(assetElement, "width"),
            Height = GetNullableIntProperty(assetElement, "height"),
            Renditions = new Dictionary<string, IAssetRendition>(renditions)
        };
    }

    private static Dictionary<string, IAssetRendition> ParseRenditions(JsonElement assetElement)
    {
        if (!assetElement.TryGetProperty("renditions", out var rendsEl) ||
            rendsEl.ValueKind is JsonValueKind.Null or not JsonValueKind.Object)
        {
            return new Dictionary<string, IAssetRendition>(StringComparer.Ordinal);
        }

        var renditions = rendsEl.EnumerateObject()
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

        return renditions;
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

    private static int? GetNullableIntProperty(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) && prop.TryGetInt32(out var value) ? value : null;

    private static DateTime? GetDateTimeProperty(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) &&
        prop.ValueKind == JsonValueKind.String &&
        prop.TryGetDateTime(out var value)
            ? value
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
            if (logger is not null)
            {
                LoggerMessages.AssetUrlParsingFailed(logger, url);
            }
            return;
        }

        // Expected: "/", "{environmentId}/", "{assetId}/", "{filename}".
        if (uri.Segments.Length < 3)
        {
            if (logger is not null)
            {
                LoggerMessages.AssetUrlParsingFailed(logger, url);
            }
            return;
        }

        var assetIdSegment = uri.Segments[2].Trim('/');
        if (Guid.TryParse(assetIdSegment, out var assetId))
        {
            dependencyContext.TrackAsset(assetId);
        }
        else if (logger is not null)
        {
            LoggerMessages.AssetUrlParsingFailed(logger, url);
        }
    }
}
