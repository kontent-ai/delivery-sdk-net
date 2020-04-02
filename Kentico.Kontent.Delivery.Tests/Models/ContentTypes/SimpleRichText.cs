using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.Tests
{
    public partial class SimpleRichText
    {
        [JsonProperty("rich_text")]
        public IRichTextContent RichText { get; set; }
    }
}
