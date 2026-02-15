using System.Text.Json;

namespace Kontent.Ai.Delivery.ContentItems.Mapping;

internal static class ContentItemJsonHelper
{
    public static string ExtractContentType(JsonElement itemElement) =>
        itemElement.TryGetProperty("system", out var system) &&
        system.TryGetProperty("type", out var type)
            ? type.GetString() ?? string.Empty
            : string.Empty;
}
