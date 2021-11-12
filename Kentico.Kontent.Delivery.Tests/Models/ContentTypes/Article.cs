using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.ContentItems.RichText;
using Newtonsoft.Json;
using NodaTime;

namespace Kentico.Kontent.Delivery.Tests.Models.ContentTypes
{
    public partial class Article : IArticle
    {
        [JsonProperty("body_copy")]
        public IRichTextContent BodyCopyRichText { get; set; }

        [JsonProperty("title")]
        [TestGreeterValueConverter]
        public string TitleConverted { get; set; }

        [JsonProperty("title")]
        [JsonIgnore]
        public string TitleIgnored { get; set; }

        [JsonProperty("title")]
        [TestGreeterValueConverter]
        public string TitleNotIgnored { get; set; }

        [JsonProperty("post_date")]
        [NodaTimeValueConverter]
        public ZonedDateTime PostDateNodaTime { get; set; }

        [JsonProperty("related_articles")]
        public IEnumerable<IArticle> RelatedArticlesInterface { get; set; }
    }
}