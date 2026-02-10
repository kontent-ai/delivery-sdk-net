using System.Text.Json;

namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// Cache payload for a single content item response, storing raw JSON strings.
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
internal sealed record CachedItemResponseRaw
{
    /// <summary>
    /// Raw JSON of the content item from the API response.
    /// </summary>
    public required string ItemJson { get; init; }

    /// <summary>
    /// Raw JSON of modular content items (linked items) keyed by codename.
    /// </summary>
    public required IReadOnlyDictionary<string, string> ModularContentJson { get; init; }

    /// <summary>
    /// Creates a cache payload by extracting raw JSON from a hydrated content item.
    /// </summary>
    internal static CachedItemResponseRaw From<TModel>(
        IContentItem<TModel> item,
        IReadOnlyDictionary<string, JsonElement>? modularContent)
    {
        return new CachedItemResponseRaw
        {
            ItemJson = CachePayloadHelper.ExtractRawJson(item),
            ModularContentJson = CachePayloadHelper.ConvertModularContentToJson(modularContent)
        };
    }
}
