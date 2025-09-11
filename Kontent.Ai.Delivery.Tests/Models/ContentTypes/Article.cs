using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems.Attributes;
using NodaTime;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes
{
    public record Article
    (
        [property: JsonPropertyName("body_copy")]
        string BodyCopy,
        
        [property: JsonPropertyName("metadata__meta_description")]
        string MetadataMetaDescription,
        
        [property: JsonPropertyName("metadata__meta_title")]
        string MetadataMetaTitle,
        
        [property: JsonPropertyName("metadata__og_description")]
        string MetadataOgDescription,
        
        [property: JsonPropertyName("metadata__og_image")]
        IEnumerable<IAsset> MetadataOgImage,
        
        [property: JsonPropertyName("metadata__og_title")]
        string MetadataOgTitle,
        
        [property: JsonPropertyName("metadata__twitter_creator")]
        string MetadataTwitterCreator,
        
        [property: JsonPropertyName("metadata__twitter_description")]
        string MetadataTwitterDescription,
        
        [property: JsonPropertyName("metadata__twitter_image")]
        IEnumerable<IAsset> MetadataTwitterImage,
        
        [property: JsonPropertyName("metadata__twitter_site")]
        string MetadataTwitterSite,
        
        [property: JsonPropertyName("metadata__twitter_title")]
        string MetadataTwitterTitle,
        
        [property: JsonPropertyName("meta_description")]
        string MetaDescription,
        
        [property: JsonPropertyName("meta_keywords")]
        string MetaKeywords,
        
        [property: JsonPropertyName("personas")]
        IEnumerable<ITaxonomyTerm> Personas,
        
        [property: JsonPropertyName("post_date")]
        DateTime? PostDate,
        
        [property: JsonPropertyName("related_articles")]
        IEnumerable<object> RelatedArticles,
        
        [property: JsonPropertyName("sitemap")]
        IEnumerable<ITaxonomyTerm> Sitemap,
        
        [property: JsonPropertyName("summary")]
        string Summary,
        
        [property: JsonPropertyName("teaser_image")]
        IEnumerable<IAsset> TeaserImage,
        
        [property: JsonPropertyName("title")]
        string Title,
        
        [property: JsonPropertyName("url_pattern")]
        string UrlPattern
    ) : IArticle
    {
        // Custom properties preserved from original Article.cs
        [PropertyName("body_copy")]
        public IRichTextContent BodyCopyRichText { get; init; }

        [PropertyName("title")]
        [TestGreeterValueConverter]
        public string TitleConverted { get; init; }

        [JsonPropertyName("title")]
        [JsonIgnore]
        public string TitleIgnored { get; init; }

        [JsonPropertyName("title")]
        [TestGreeterValueConverter]
        public string TitleNotIgnored { get; init; }

        [PropertyName("post_date")]
        public IDateTimeContent PostDateContent { get; init; }

        [PropertyName("post_date")]
        [NodaTimeValueConverter]
        public ZonedDateTime PostDateNodaTime { get; init; }

        [PropertyName("related_articles")]
        public List<IArticle> RelatedArticlesInterface { get; init; }

        [PropertyName("related_articles")]
        [TestLinkedItemCodenamesValueConverter]
        public List<string> RelatedArticleCodenames { get; init; }
    }
}