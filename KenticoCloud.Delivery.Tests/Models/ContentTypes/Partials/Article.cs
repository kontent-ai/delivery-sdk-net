using Newtonsoft.Json;

namespace KenticoCloud.Delivery.Tests
{
    public partial class Article
    {
        [JsonProperty("body_copy")]
        public IRichTextContent BodyCopyRichText { get; set; }

        [JsonProperty("title")]
        [TestGreeterValueConverter]
        public string TitleConverted { get; set; }
    }
}
