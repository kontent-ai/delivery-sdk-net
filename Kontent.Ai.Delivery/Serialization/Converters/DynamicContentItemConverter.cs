using System.Text.Json;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.ContentItems;

namespace Kontent.Ai.Delivery.Serialization.Converters;

/// <summary>
/// Deserializes ContentItem in dynamic mode, preserving full element structure.
/// Used when the model type is IElementsModel or DynamicElements.
/// Elements are stored as raw JsonElements containing type, name, and value properties.
/// </summary>
internal sealed class DynamicContentItemConverter<TModel> : JsonConverter<ContentItem<TModel>>
    where TModel : IElementsModel
{
    public override ContentItem<TModel> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // 1. Parse system attributes
        var systemJson = root.GetProperty("system").GetRawText();
        var system = JsonSerializer.Deserialize<ContentItemSystemAttributes>(systemJson, options)!;

        // 2. Get elements JsonElement
        var elementsElement = root.TryGetProperty("elements", out var els)
            ? els
            : default;

        // 3. Capture raw elements for post-processing
        var rawElements = elementsElement.ValueKind != JsonValueKind.Undefined
            && elementsElement.ValueKind != JsonValueKind.Null
            ? elementsElement.CloneElement()
            : (JsonElement?)null;

        // 4. Parse elements in dynamic mode - preserve full structure
        var elements = ParseDynamicElements(elementsElement);

        return new ContentItem<TModel>
        {
            System = system,
            Elements = elements,
            RawElements = rawElements
        };
    }

    public override void Write(Utf8JsonWriter writer, ContentItem<TModel> value, JsonSerializerOptions options)
        => throw new NotSupportedException("Serialization of ContentItem is not supported.");

    /// <summary>
    /// Parses elements in dynamic mode, preserving full element structure (type, name, value).
    /// Each element is stored as a complete JsonElement for flexible runtime access.
    /// </summary>
    private static TModel ParseDynamicElements(JsonElement elementsElement)
    {
        var map = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        if (elementsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in elementsElement.EnumerateObject())
            {
                // Store the WHOLE element object (clone to ensure lifetime)
                map[prop.Name] = prop.Value.CloneElement();
            }
        }

        return (TModel)(IElementsModel)new DynamicElements(map);
    }
}
