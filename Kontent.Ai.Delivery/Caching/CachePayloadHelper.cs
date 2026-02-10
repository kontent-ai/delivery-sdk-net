using System.Text.Json;
using Kontent.Ai.Delivery.ContentItems;

namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// Shared helpers for building cache payloads from content items and modular content.
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

        if (!rawItem.RawItemJson.HasValue)
        {
            throw new InvalidOperationException(
                $"Content item '{item.System.Codename}' has null RawItemJson. " +
                "This may indicate the item was created manually rather than deserialized from API response.");
        }

        return rawItem.RawItemJson.Value.GetRawText();
    }
}
