using System.Text.Json;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Mapping;

namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// Rehydrates cached raw JSON payloads into strongly-typed content items
/// using the existing deserialization and hydration pipeline.
/// </summary>
internal static class CachePayloadRehydrator
{
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
        CachedItemResponseRaw payload,
        IContentDeserializer contentDeserializer,
        ContentItemMapper contentItemMapper,
        bool isDynamicModel,
        CancellationToken cancellationToken)
    {
        using var itemDoc = JsonDocument.Parse(payload.ItemJson);
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
        CachedItemListingResponseRaw payload,
        IContentDeserializer contentDeserializer,
        ContentItemMapper contentItemMapper,
        bool isDynamicModel,
        CancellationToken cancellationToken)
    {
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
