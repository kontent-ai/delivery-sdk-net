using System.Text.Json;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.ContentTypes.Element;

namespace Kontent.Ai.Delivery.Serialization.Converters;

/// <summary>
/// JSON converter for dictionaries of ContentElement that hydrates the Codename property
/// from the dictionary key. This is needed because in ContentType responses, the element
/// codename is the dictionary key, not a JSON property within the element value.
/// </summary>
internal sealed class ContentElementDictionaryConverter : JsonConverter<IReadOnlyDictionary<string, ContentElement>>
{
    public override IReadOnlyDictionary<string, ContentElement> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of object for elements dictionary.");
        }

        var dict = new Dictionary<string, ContentElement>(StringComparer.Ordinal);

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name (element codename).");
            }

            var codename = reader.GetString()
                ?? throw new JsonException("Element codename cannot be null.");

            reader.Read();

            // Deserialize element - ContentElementConverter will handle polymorphism
            var element = JsonSerializer.Deserialize<ContentElement>(ref reader, options)
                ?? throw new JsonException($"Failed to deserialize element '{codename}'.");

            // Hydrate codename from dictionary key using record 'with' expression
            var elementWithCodename = element with { Codename = codename };
            dict[codename] = elementWithCodename;
        }

        return dict;
    }

    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, ContentElement> value,
        JsonSerializerOptions options)
        => throw new NotSupportedException("Serialization of ContentElement dictionary is not supported.");
}
