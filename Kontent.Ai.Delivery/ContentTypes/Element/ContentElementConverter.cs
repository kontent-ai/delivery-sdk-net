using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentTypes.Element;

/// <summary>
/// Serializes content element definitions into specific types
/// </summary>
public class ContentElementConverter : JsonConverter<IContentElement>
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(IContentElement).IsAssignableFrom(typeToConvert);
    }

    /// <inheritdoc/>
    public override IContentElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;

        var elementType = root.GetProperty("type").GetString() switch
        {
            "taxonomy" => typeof(TaxonomyElement),
            "multiple_choice" => typeof(MultipleChoiceElement),
            _ => typeof(ContentElement)
        };

        var jsonText = root.GetRawText();
        return (IContentElement)JsonSerializer.Deserialize(jsonText, elementType, options);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IContentElement value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
