using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes
{
    public record ArticleWithImplementedTypes
    (
        [property: JsonPropertyName("personas")]
        IEnumerable<ITaxonomyTerm> Personas,
        
        [property: JsonPropertyName("title")]
        string Title,
        
        [property: JsonPropertyName("teaser_image")]
        IEnumerable<IAsset> TeaserImage,
        
        [property: JsonPropertyName("post_date")]
        DateTime? PostDate,
        
        [property: JsonPropertyName("summary")]
        string Summary,
        
        [property: JsonPropertyName("body_copy")]
        IRichTextContent BodyCopy,
        
        [property: JsonPropertyName("related_articles")]
        IEnumerable<object> RelatedArticles,
        
        [property: JsonPropertyName("meta_keywords")]
        string MetaKeywords,
        
        [property: JsonPropertyName("meta_description")]
        string MetaDescription,
        
        [property: JsonPropertyName("url_pattern")]
        string UrlPattern
    );
}