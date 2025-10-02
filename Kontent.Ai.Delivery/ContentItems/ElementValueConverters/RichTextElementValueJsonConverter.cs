using System.Text.Json;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.ContentItems.ContentLinks;
using Kontent.Ai.Delivery.ContentItems.Elements;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

namespace Kontent.Ai.Delivery.ContentItems;

internal class RichTextElementValueJsonConverter : JsonConverter<RichTextElementValue>
{
    public string? ElementCodename { get; set; }

    public override RichTextElementValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            return null;

        return new RichTextElementValue
        {
            Type = root.GetProperty("type").GetString() ?? string.Empty,
            Name = root.GetProperty("name").GetString() ?? string.Empty,
            Codename = ElementCodename ?? string.Empty,
            Value = root.GetProperty("value").GetString() ?? string.Empty,
            Images = DeserializeImages(root, options),
            Links = DeserializeLinks(root, options),
            ModularContent = DeserializeModularContent(root)
        };
    }

    public override void Write(Utf8JsonWriter writer, RichTextElementValue value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", value.Type);
        writer.WriteString("name", value.Name);
        writer.WriteString("value", value.Value);

        writer.WritePropertyName("images");
        JsonSerializer.Serialize(writer, value.Images, options);

        writer.WritePropertyName("links");
        JsonSerializer.Serialize(writer, value.Links, options);

        writer.WritePropertyName("modular_content");
        JsonSerializer.Serialize(writer, value.ModularContent, options);

        writer.WriteEndObject();
    }

    private static IDictionary<Guid, InlineImage> DeserializeImages(JsonElement root, JsonSerializerOptions options) =>
        root.TryGetProperty("images", out var imagesEl) && imagesEl.ValueKind == JsonValueKind.Object
            ? imagesEl.EnumerateObject()
                .Where(prop => Guid.TryParse(prop.Name, out _))
                .Select(prop => (Id: Guid.Parse(prop.Name), Image: JsonSerializer.Deserialize<InlineImage>(prop.Value.GetRawText(), options)))
                .Where(x => x.Image is not null)
                .ToDictionary(x => x.Id, x => x.Image!)
            : new Dictionary<Guid, InlineImage>();

    private static IDictionary<Guid, ContentLink> DeserializeLinks(JsonElement root, JsonSerializerOptions options) =>
        root.TryGetProperty("links", out var linksEl) && linksEl.ValueKind == JsonValueKind.Object
            ? linksEl.EnumerateObject()
                .Where(prop => Guid.TryParse(prop.Name, out _))
                .Select(prop => (Id: Guid.Parse(prop.Name), Link: JsonSerializer.Deserialize<ContentLink>(prop.Value.GetRawText(), options)))
                .Where(x => x.Link is not null)
                .ToDictionary(x => x.Id, x => x.Link!)
            : new Dictionary<Guid, ContentLink>();

    private static List<string> DeserializeModularContent(JsonElement root) =>
        root.TryGetProperty("modular_content", out var modularEl) && modularEl.ValueKind == JsonValueKind.Array
            ? JsonSerializer.Deserialize<List<string>>(modularEl.GetRawText()) ?? []
            : [];
}
