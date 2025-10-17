using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes;

public record ArticleWithImplementedTypes
(
    [property: JsonPropertyName("personas")]
    IEnumerable<TaxonomyTerm> Personas,

    [property: JsonPropertyName("title")]
    string Title,

    [property: JsonPropertyName("teaser_image")]
    IEnumerable<Asset> TeaserImage,

    [property: JsonPropertyName("post_date")]
    DateTime? PostDate,

    [property: JsonPropertyName("summary")]
    string Summary,

    [property: JsonPropertyName("body_copy")]
    IRichTextContent BodyCopy,

    [property: JsonPropertyName("related_articles")]
    IEnumerable<string> RelatedArticles,

    [property: JsonPropertyName("meta_keywords")]
    string MetaKeywords,

    [property: JsonPropertyName("meta_description")]
    string MetaDescription,

    [property: JsonPropertyName("url_pattern")]
    string UrlPattern
);