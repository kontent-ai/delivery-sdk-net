using Kentico.Kontent.Delivery.Abstractions.ContentItems.RichText;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.Tests.Models.ContentTypes
{
    public class SimpleRichText
    {
        [JsonProperty("rich_text")]
        public IRichTextContent RichText { get; set; }

        [JsonProperty("rich_text")]
        public string RichTextString { get; set; }
    }
}
