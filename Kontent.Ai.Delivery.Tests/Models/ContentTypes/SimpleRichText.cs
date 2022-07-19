using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes
{
    public class SimpleRichText
    {
        [JsonProperty("rich_text")]
        public IRichTextContent RichText { get; set; }

        [JsonProperty("rich_text")]
        public string RichTextString { get; set; }
    }
}
