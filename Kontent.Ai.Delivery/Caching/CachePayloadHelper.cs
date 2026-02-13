using System.Text.Json;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Mapping;

namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// Shared helpers for building and rehydrating cache payloads.
/// </summary>
internal static class CachePayloadHelper
{
    internal static IReadOnlyDictionary<string, string> ConvertModularContentToJson(
        IReadOnlyDictionary<string, JsonElement>? modularContent)
    {
        if (modularContent is null || modularContent.Count == 0)
            return new Dictionary<string, string>();

        var result = new Dictionary<string, string>(modularContent.Count, StringComparer.Ordinal);
        foreach (var (codename, element) in modularContent)
        {
            result[codename] = element.GetRawText();
        }

        return result;
    }

    /// <summary>
    /// Extracts the raw JSON string from a content item, validating that raw JSON is available.
    /// </summary>
    internal static string ExtractRawJson(IContentItem item)
    {
        var rawItem = item as IRawContentItem
            ?? throw new InvalidOperationException(
                $"Content item '{item.System.Codename}' does not have raw JSON available. " +
                "Ensure the item was deserialized through the SDK's standard pipeline.");

        return !rawItem.RawItemJson.HasValue
            ? throw new InvalidOperationException(
                $"Content item '{item.System.Codename}' has null RawItemJson. " +
                "This may indicate the item was created manually rather than deserialized from API response.")
            : rawItem.RawItemJson.Value.GetRawText();
    }

    /// <summary>
    /// Parses modular content from cached raw JSON strings back into <see cref="JsonElement"/> values.
    /// </summary>
    internal static IReadOnlyDictionary<string, JsonElement> ParseModularContent(
        IReadOnlyDictionary<string, string> modularContentJson)
    {
        if (modularContentJson.Count == 0)
            return new Dictionary<string, JsonElement>();

        var result = new Dictionary<string, JsonElement>(modularContentJson.Count, StringComparer.Ordinal);
        foreach (var (codename, json) in modularContentJson)
        {
            using var doc = JsonDocument.Parse(json);
            result[codename] = doc.RootElement.Clone();
        }

        return result;
    }

    /// <summary>
    /// Rehydrates a single content item from a cached raw JSON payload.
    /// </summary>
    internal static async Task<ContentItem<TModel>> RehydrateItemAsync<TModel>(
        CachedRawItemsPayload payload,
        IContentDeserializer contentDeserializer,
        ContentItemMapper contentItemMapper,
        bool isDynamicModel,
        CancellationToken cancellationToken)
    {
        if (payload.ItemsJson.Count != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one cached item for item query rehydration, but found {payload.ItemsJson.Count}.");
        }

        using var itemDoc = JsonDocument.Parse(payload.ItemsJson[0]);
        var item = (ContentItem<TModel>)contentDeserializer.DeserializeContentItem(
            itemDoc.RootElement.Clone(), typeof(TModel));

        if (!isDynamicModel)
        {
            var modularContent = ParseModularContent(payload.ModularContentJson);
            await contentItemMapper.CompleteItemAsync(item, modularContent, dependencyContext: null, cancellationToken)
                .ConfigureAwait(false);
        }

        return item;
    }

    /// <summary>
    /// Rehydrates a listing response from a cached raw JSON payload.
    /// </summary>
    internal static async Task<DeliveryItemListingResponse<TModel>> RehydrateListingAsync<TModel>(
        CachedRawItemsPayload payload,
        IContentDeserializer contentDeserializer,
        ContentItemMapper contentItemMapper,
        bool isDynamicModel,
        CancellationToken cancellationToken)
    {
        if (payload.Pagination is null)
        {
            throw new InvalidOperationException("Cached listing payload is missing pagination.");
        }

        var modularContent = ParseModularContent(payload.ModularContentJson);
        var items = new List<ContentItem<TModel>>(payload.ItemsJson.Count);

        foreach (var itemJson in payload.ItemsJson)
        {
            using var itemDoc = JsonDocument.Parse(itemJson);
            var item = (ContentItem<TModel>)contentDeserializer.DeserializeContentItem(
                itemDoc.RootElement.Clone(), typeof(TModel));

            if (!isDynamicModel)
            {
                await contentItemMapper.CompleteItemAsync(item, modularContent, dependencyContext: null, cancellationToken)
                    .ConfigureAwait(false);
            }

            items.Add(item);
        }

        return new DeliveryItemListingResponse<TModel>
        {
            Items = items,
            Pagination = payload.Pagination,
            ModularContent = modularContent
        };
    }
}
