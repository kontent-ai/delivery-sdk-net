using System.Text.Json;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.ContentTypes.Element;

namespace Kontent.Ai.Delivery.Serialization.Converters;

/// <summary>
/// Polymorphic JSON converter for ContentElement types.
/// Routes deserialization to the appropriate concrete type based on the "type" field:
/// - "taxonomy" → TaxonomyElement
/// - "multiple_choice" → MultipleChoiceElement
/// - Others → ContentElement (base type)
/// </summary>
internal sealed class ContentElementConverter : JsonConverter<ContentElement>
{
    public override ContentElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var elementType = root.TryGetProperty("type", out var typeEl)
            ? typeEl.GetString()
            : null;

        return elementType switch
        {
            // Derived types don't trigger this converter (JsonConverter<T> only matches exact type)
            "taxonomy" => JsonSerializer.Deserialize<TaxonomyElement>(root, options)
                ?? throw new JsonException("Failed to deserialize taxonomy element."),
            "multiple_choice" => JsonSerializer.Deserialize<MultipleChoiceElement>(root, options)
                ?? throw new JsonException("Failed to deserialize multiple choice element."),
            // Base type: deserialize manually to avoid infinite recursion
            _ => DeserializeBaseElement(root)
        };
    }

    public override void Write(Utf8JsonWriter writer, ContentElement value, JsonSerializerOptions options)
        => throw new NotSupportedException("Serialization of ContentElement is not supported.");

    private static ContentElement DeserializeBaseElement(JsonElement root)
    {
        return new ContentElement
        {
            Type = root.GetProperty("type").GetString()
                ?? throw new JsonException("Content element 'type' cannot be null."),
            Name = root.GetProperty("name").GetString()
                ?? throw new JsonException("Content element 'name' cannot be null."),
            Codename = root.TryGetProperty("codename", out var codename)
                ? codename.GetString() ?? string.Empty
                : string.Empty
        };
    }
}
