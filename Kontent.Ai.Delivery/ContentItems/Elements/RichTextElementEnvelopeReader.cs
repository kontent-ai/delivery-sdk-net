using System.Text.Json;
using Kontent.Ai.Delivery.ContentItems.ContentLinks;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

namespace Kontent.Ai.Delivery.ContentItems.Elements;

internal static class RichTextElementEnvelopeReader
{
    public static RichTextElementData Read(
        JsonElement envelope,
        string codename,
        JsonSerializerOptions? serializerOptions = null,
        bool preserveEmptyModularContentEntries = false)
    {
        return new RichTextElementData
        {
            Type = GetStringProperty(envelope, "type"),
            Name = GetStringProperty(envelope, "name"),
            Codename = codename,
            Value = GetStringProperty(envelope, "value"),
            Images = DeserializeInlineImages(envelope, serializerOptions),
            Links = DeserializeContentLinks(envelope, serializerOptions),
            ModularContent = DeserializeModularContent(envelope, serializerOptions, preserveEmptyModularContentEntries)
        };
    }

    public static string GetStringProperty(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var prop)
            ? prop.GetString() ?? string.Empty
            : string.Empty;

    private static Dictionary<Guid, IInlineImage> DeserializeInlineImages(
        JsonElement root,
        JsonSerializerOptions? serializerOptions)
    {
        if (!root.TryGetProperty("images", out var imagesEl) || imagesEl.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var result = new Dictionary<Guid, IInlineImage>();
        foreach (var prop in imagesEl.EnumerateObject())
        {
            if (!Guid.TryParse(prop.Name, out var id))
            {
                continue;
            }

            var image = serializerOptions is null
                ? JsonSerializer.Deserialize<InlineImage>(prop.Value)
                : JsonSerializer.Deserialize<InlineImage>(prop.Value, serializerOptions);
            if (image is not null)
            {
                result[id] = image;
            }
        }

        return result;
    }

    private static Dictionary<Guid, IContentLink> DeserializeContentLinks(
        JsonElement root,
        JsonSerializerOptions? serializerOptions)
    {
        if (!root.TryGetProperty("links", out var linksEl) || linksEl.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var result = new Dictionary<Guid, IContentLink>();
        foreach (var prop in linksEl.EnumerateObject())
        {
            if (!Guid.TryParse(prop.Name, out var id))
            {
                continue;
            }

            var link = serializerOptions is null
                ? JsonSerializer.Deserialize<ContentLink>(prop.Value)
                : JsonSerializer.Deserialize<ContentLink>(prop.Value, serializerOptions);
            if (link is null)
            {
                continue;
            }

            link.Id = id;
            result[id] = link;
        }

        return result;
    }

    private static List<string> DeserializeModularContent(
        JsonElement root,
        JsonSerializerOptions? serializerOptions,
        bool preserveEmptyEntries)
    {
        if (!root.TryGetProperty("modular_content", out var modularEl) || modularEl.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        if (preserveEmptyEntries)
        {
            var deserializedModularContent = serializerOptions is null
                ? JsonSerializer.Deserialize<List<string>>(modularEl)
                : JsonSerializer.Deserialize<List<string>>(modularEl, serializerOptions);
            return deserializedModularContent ?? [];
        }

        List<string> list = [];
        foreach (var item in modularEl.EnumerateArray())
        {
            var str = item.GetString();
            if (!string.IsNullOrEmpty(str))
            {
                list.Add(str);
            }
        }

        return list;
    }
}
