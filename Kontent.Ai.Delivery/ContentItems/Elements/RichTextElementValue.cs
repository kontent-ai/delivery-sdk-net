using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.ContentItems.ContentLinks;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

namespace Kontent.Ai.Delivery.ContentItems.Elements;

internal class RichTextElementValue : ContentElementValue<string>, IRichTextElementValue
{
    [JsonPropertyName("images")]
    public required IDictionary<Guid, InlineImage> Images { get; set; }

    [JsonPropertyName("links")]
    public required IDictionary<Guid, ContentLink> Links { get; set; }

    [JsonPropertyName("modular_content")]
    public required List<string> ModularContent { get; set; }

    IDictionary<Guid, IInlineImage> IRichTextElementValue.Images
        => Images.ToDictionary(kvp => kvp.Key, kvp => (IInlineImage)kvp.Value); // TODO: improve

    IDictionary<Guid, IContentLink> IRichTextElementValue.Links
        => Links.ToDictionary(kvp => kvp.Key, kvp => (IContentLink)kvp.Value);
}
