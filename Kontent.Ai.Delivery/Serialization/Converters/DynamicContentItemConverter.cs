using System.Text.Json;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.ContentItems;

namespace Kontent.Ai.Delivery.Serialization.Converters;

/// <summary>
/// Deserializes ContentItem in dynamic mode, preserving full element structure.
/// Used when the model type is IDynamicElements or DynamicElements.
/// Elements are stored as raw JsonElements containing type, name, and value properties.
/// </summary>
internal sealed class DynamicContentItemConverter<TModel> : JsonConverter<ContentItem<TModel>>
{
    public override ContentItem<TModel> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // 1. Parse system attributes
        if (!root.TryGetProperty("system", out var systemElement))
        {
            throw new JsonException("Missing required 'system' property in content item JSON.");
        }

        var system = JsonSerializer.Deserialize<ContentItemSystemAttributes>(systemElement, options)
            ?? throw new JsonException("Failed to deserialize 'system' property in content item JSON.");

        // 2. Get elements JsonElement
        var elementsElement = root.TryGetProperty("elements", out var els)
            ? els
            : default;

        // 3. Capture full item JSON for post-processing and runtime type resolution
        var rawItemJson = root.Clone();

        // 4. Parse elements in dynamic mode - preserve full structure
        var elements = ParseDynamicElements(elementsElement);

        return new ContentItem<TModel>
        {
            System = system,
            Elements = elements,
            RawItemJson = rawItemJson
        };
    }

    public override void Write(Utf8JsonWriter writer, ContentItem<TModel> value, JsonSerializerOptions options)
        => throw new NotSupportedException("Serialization of ContentItem is not supported.");

    /// <summary>
    /// Parses elements in dynamic mode, preserving full element structure (type, name, value).
    /// Each element is stored as a complete JsonElement for flexible runtime access.
    /// </summary>
    private static TModel ParseDynamicElements(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return (TModel)(IDynamicElements)new DynamicElements(
                new Dictionary<string, JsonElement>(StringComparer.Ordinal));
        }

        // Clone the parent once - children share the cloned backing memory
        var clonedElements = element.Clone();

        var map = clonedElements.EnumerateObject()
            .ToDictionary(
                prop => prop.Name,
                prop => prop.Value,
                StringComparer.Ordinal);

        return (TModel)(IDynamicElements)new DynamicElements(map);
    }
}