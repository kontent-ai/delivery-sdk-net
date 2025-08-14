using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.ContentItems.Elements
{
    internal class RichTextElementValue : ContentElementValue<string>, IRichTextElementValue
    {
        [JsonProperty("images")]
        public required IDictionary<Guid, IInlineImage> Images { get; set; }

        [JsonProperty("links")]
        public required IDictionary<Guid, IContentLink> Links { get; set; }

        [JsonProperty("modular_content")]
        public required List<string> ModularContent { get; set; }
    }
}
