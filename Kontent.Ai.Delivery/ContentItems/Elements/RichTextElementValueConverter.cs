using System.Text.Json;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.ContentItems.ContentLinks;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

namespace Kontent.Ai.Delivery.ContentItems.Elements;

internal class RichTextElementValueConverter : JsonConverter<RichTextElementValue> // TODO: improve and consider simplifying and merging with other rich text tooling
{
    public string? ElementCodename { get; set; }

    public override RichTextElementValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var element = new RichTextElementValue
        {
            Type = root.GetProperty("type").GetString() ?? string.Empty,
            Name = root.GetProperty("name").GetString() ?? string.Empty,
            Codename = ElementCodename ?? string.Empty,
            Value = root.GetProperty("value").GetString() ?? string.Empty,
            Images = DeserializeImages(root),
            Links = DeserializeLinks(root),
            ModularContent = DeserializeModularContent(root)
        };

        return element;
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

    private static IDictionary<Guid, InlineImage> DeserializeImages(JsonElement root)
    {
        if (!root.TryGetProperty("images", out var imagesEl) || imagesEl.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<Guid, InlineImage>();
        }

        var images = new Dictionary<Guid, InlineImage>();
        foreach (var prop in imagesEl.EnumerateObject())
        {
            if (Guid.TryParse(prop.Name, out var guid))
            {
                var image = JsonSerializer.Deserialize<InlineImage>(prop.Value.GetRawText());
                if (image != null)
                {
                    images[guid] = image;
                }
            }
        }
        return images;
    }

    private static IDictionary<Guid, ContentLink> DeserializeLinks(JsonElement root)
    {
        if (!root.TryGetProperty("links", out var linksEl) || linksEl.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<Guid, ContentLink>();
        }

        var links = new Dictionary<Guid, ContentLink>();
        foreach (var prop in linksEl.EnumerateObject())
        {
            if (Guid.TryParse(prop.Name, out var guid))
            {
                var link = JsonSerializer.Deserialize<ContentLink>(prop.Value.GetRawText());
                if (link != null)
                {
                    links[guid] = link;
                }
            }
        }
        return links;
    }

    private static List<string> DeserializeModularContent(JsonElement root)
    {
        if (!root.TryGetProperty("modular_content", out var modularEl) || modularEl.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        return JsonSerializer.Deserialize<List<string>>(modularEl.GetRawText()) ?? new List<string>();
    }
}