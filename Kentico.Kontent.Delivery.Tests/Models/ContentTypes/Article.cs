using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentItems.Attributes;
using Newtonsoft.Json;
using NodaTime;

namespace Kentico.Kontent.Delivery.Tests.Models.ContentTypes
{
    public partial class Article : IArticle
    {
        [PropertyName("body_copy")]
        public IRichTextContent BodyCopyRichText { get; set; }

        [PropertyName("title")]
        [TestGreeterValueConverter]
        public string TitleConverted { get; set; }

        [JsonProperty("title")]
        [JsonIgnore]
        public string TitleIgnored { get; set; }

        [JsonProperty("title")]
        [TestGreeterValueConverter]
        public string TitleNotIgnored { get; set; }

        [PropertyName("post_date")]
        [NodaTimeValueConverter]
        public ZonedDateTime PostDateNodaTime { get; set; }

        [PropertyName("related_articles")]
        public List<IArticle> RelatedArticlesInterface { get; set; }

        [PropertyName("related_articles")]
        [TestLinkedItemCodenamesValueConverter]
        public List<string> RelatedArticleCodenames { get; set; }
    }
}