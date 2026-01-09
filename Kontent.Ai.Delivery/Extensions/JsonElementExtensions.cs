using System.Text.Json;

namespace Kontent.Ai.Delivery.Extensions;

/// <summary>
/// Extension methods for JsonElement to support content item deserialization.
/// </summary>
internal static class JsonElementExtensions
{
    /// <summary>
    /// Clones a JsonElement to ensure it survives beyond the JsonDocument lifetime.
    /// Required when storing JsonElements extracted from a JsonDocument that will be disposed.
    /// </summary>
    public static JsonElement CloneElement(this JsonElement element)
    {
        using var doc = JsonDocument.Parse(element.GetRawText());
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Extracts the "type" property value from a content element JsonElement.
    /// Returns the element type (e.g., "text", "rich_text", "asset", "taxonomy")
    /// or null if the property is missing or not a string.
    /// </summary>
    public static string? GetElementType(this JsonElement element)
    {
        return element.TryGetProperty("type", out var typeProp)
            && typeProp.ValueKind == JsonValueKind.String
            ? typeProp.GetString()
            : null;
    }

    /// <summary>
    /// Checks if an element type requires complex post-processing by ContentItemMapper.
    /// Complex types (rich_text, taxonomy, asset, modular_content) are set to null during initial deserialization
    /// and hydrated later with fully-parsed objects.
    /// </summary>
    public static bool IsComplexElementType(string? elementType)
        => string.Equals(elementType, "rich_text", StringComparison.OrdinalIgnoreCase)
        || string.Equals(elementType, "taxonomy", StringComparison.OrdinalIgnoreCase)
        || string.Equals(elementType, "asset", StringComparison.OrdinalIgnoreCase)
        || string.Equals(elementType, "modular_content", StringComparison.OrdinalIgnoreCase);
}