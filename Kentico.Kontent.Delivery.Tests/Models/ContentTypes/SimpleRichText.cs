using Kentico.Kontent.Delivery.Abstractions.Models.RichText;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.Tests.Models.ContentTypes
{
    public partial class SimpleRichText
    {
        [JsonProperty("rich_text")]
        public IRichTextContent RichText { get; set; }

        [JsonProperty("rich_text")]
        public string RichTextString { get; set; }
    }
}
