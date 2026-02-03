using System.Text.Json;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.ContentItems.ContentLinks;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

namespace Kontent.Ai.Delivery.ContentItems.Elements;

/// <summary>
/// JSON converter for deserializing <see cref="RichTextElementData"/> from Kontent.ai API response format.
/// </summary>
/// <remarks>
/// Expected JSON structure:
/// <code>
/// {
///   "type": "rich_text",
///   "name": "Body Copy",
///   "value": "&lt;p&gt;Hello world&lt;/p&gt;",
///   "images": {
///     "abc-123": { "image_id": "abc-123", "url": "...", "description": "...", "width": 100, "height": 100 }
///   },
///   "links": {
///     "def-456": { "codename": "linked_item", "type": "article", "url_slug": "..." }
///   },
///   "modular_content": ["component1", "linked_item"]
/// }
/// </code>
/// </remarks>
internal sealed class RichTextElementDataConverter : JsonConverter<RichTextElementData>
{
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public override RichTextElementData? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Parse required string properties
        var type = GetStringProperty(root, "type");
        var name = GetStringProperty(root, "name");
        var value = GetStringProperty(root, "value");

        // Codename may not always be present in the element JSON
        var codename = root.TryGetProperty("codename", out var codenameEl)
            ? codenameEl.GetString() ?? string.Empty
            : string.Empty;

        // Parse images dictionary
        var images = DeserializeInlineImages(root);

        // Parse links dictionary
        var links = DeserializeContentLinks(root);

        // Parse modular_content array
        var modularContent = DeserializeModularContent(root);

        return new RichTextElementData
        {
            Type = type,
            Name = name,
            Codename = codename,
            Value = value,
            Images = images,
            Links = links,
            ModularContent = modularContent
        };
    }

    public override void Write(Utf8JsonWriter writer, RichTextElementData value, JsonSerializerOptions options)
        => throw new NotSupportedException("Serialization of RichTextElementData is not supported.");

    private static string GetStringProperty(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var prop)
            ? prop.GetString() ?? string.Empty
            : string.Empty;

    private static Dictionary<Guid, IInlineImage> DeserializeInlineImages(JsonElement root)
    {
        if (!root.TryGetProperty("images", out var imagesEl) || imagesEl.ValueKind != JsonValueKind.Object)
            return [];

        var result = new Dictionary<Guid, IInlineImage>();
        foreach (var prop in imagesEl.EnumerateObject())
        {
            if (Guid.TryParse(prop.Name, out var id))
            {
                var image = JsonSerializer.Deserialize<InlineImage>(prop.Value, CaseInsensitiveOptions);
                if (image is not null)
                    result[id] = image;
            }
        }

        return result;
    }

    private static Dictionary<Guid, IContentLink> DeserializeContentLinks(JsonElement root)
    {
        if (!root.TryGetProperty("links", out var linksEl) || linksEl.ValueKind != JsonValueKind.Object)
            return [];

        var result = new Dictionary<Guid, IContentLink>();
        foreach (var prop in linksEl.EnumerateObject())
        {
            if (Guid.TryParse(prop.Name, out var id))
            {
                var link = JsonSerializer.Deserialize<ContentLink>(prop.Value, CaseInsensitiveOptions);
                if (link is not null)
                {
                    link.Id = id;
                    result[id] = link;
                }
            }
        }

        return result;
    }

    private static List<string> DeserializeModularContent(JsonElement root)
    {
        if (!root.TryGetProperty("modular_content", out var modularEl) || modularEl.ValueKind != JsonValueKind.Array)
            return [];

        List<string> list = [];
        foreach (var item in modularEl.EnumerateArray())
        {
            var str = item.GetString();
            if (!string.IsNullOrEmpty(str))
                list.Add(str);
        }

        return list;
    }
}
