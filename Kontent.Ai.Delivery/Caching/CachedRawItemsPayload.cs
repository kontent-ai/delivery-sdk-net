using System.Text.Json;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// Cache payload for item and listing responses, storing raw JSON strings.
/// </summary>
/// <remarks>
/// <para>
/// This record stores the raw JSON from the API response rather than hydrated C# objects.
/// This approach solves several distributed caching issues:
/// </para>
/// <list type="bullet">
/// <item><description>Raw JSON has no C# object cycles (it's just text)</description></item>
/// <item><description>Raw JSON serializes trivially without ReferenceHandler.Preserve overhead</description></item>
/// <item><description>Avoids serialization failures on complex types (rich text, custom converters)</description></item>
/// </list>
/// <para>
/// On cache hit, the raw JSON is rehydrated to the requested type using
/// the existing deserialization and hydration pipeline (IContentDeserializer and ContentItemMapper).
/// </para>
/// </remarks>
internal sealed record CachedRawItemsPayload
{
    /// <summary>
    /// Raw JSON of each content item from the API response.
    /// Item queries store exactly one entry.
    /// </summary>
    public required IReadOnlyList<string> ItemsJson { get; init; }

    /// <summary>
    /// Raw JSON of modular content items (linked items) keyed by codename.
    /// </summary>
    public required IReadOnlyDictionary<string, string> ModularContentJson { get; init; }

    /// <summary>
    /// Pagination information from listing responses.
    /// Null for single-item responses.
    /// </summary>
    public Pagination? Pagination { get; init; }

    /// <summary>
    /// Creates a cache payload by extracting raw JSON from a hydrated content item.
    /// </summary>
    internal static CachedRawItemsPayload FromItem<TModel>(
        IContentItem<TModel> item,
        IReadOnlyDictionary<string, JsonElement>? modularContent)
    {
        return new CachedRawItemsPayload
        {
            ItemsJson = [CachePayloadHelper.ExtractRawJson(item)],
            ModularContentJson = CachePayloadHelper.ConvertModularContentToJson(modularContent),
            Pagination = null
        };
    }

    /// <summary>
    /// Creates a cache payload by extracting raw JSON from a listing response.
    /// </summary>
    internal static CachedRawItemsPayload FromListing<TModel>(
        DeliveryItemListingResponse<TModel> response)
    {
        var itemsJson = new List<string>(response.Items.Count);

        foreach (var item in response.Items)
        {
            itemsJson.Add(CachePayloadHelper.ExtractRawJson(item));
        }

        return new CachedRawItemsPayload
        {
            ItemsJson = itemsJson,
            ModularContentJson = CachePayloadHelper.ConvertModularContentToJson(response.ModularContent),
            Pagination = response.Pagination
        };
    }
}
