using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems.Elements;

internal class RichTextElementValue : ContentElementValue<string>, IRichTextElementValue
{
    [JsonPropertyName("images")]
    public required IDictionary<Guid, IInlineImage> Images { get; set; }

    [JsonPropertyName("links")]
    public required IDictionary<Guid, IContentLink> Links { get; set; }

    [JsonPropertyName("modular_content")]
    public required List<string> ModularContent { get; set; }
}
