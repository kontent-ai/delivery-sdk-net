using System.Text.Json;
using System.Text.Json.Serialization;

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

        // Codename may not always be present in the element JSON
        var codename = root.TryGetProperty("codename", out var codenameEl)
            ? codenameEl.GetString() ?? string.Empty
            : string.Empty;

        return RichTextElementEnvelopeReader.Read(
            root,
            codename,
            CaseInsensitiveOptions,
            preserveEmptyModularContentEntries: false);
    }

    public override void Write(Utf8JsonWriter writer, RichTextElementData value, JsonSerializerOptions options)
        => throw new NotSupportedException("Serialization of RichTextElementData is not supported.");
}
