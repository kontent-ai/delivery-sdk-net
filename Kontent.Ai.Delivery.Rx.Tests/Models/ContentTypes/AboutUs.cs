using System.Collections.Generic;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Rx.Tests.Models.ContentTypes;
public record AboutUs
(
    [property: JsonPropertyName("facts")]
    IEnumerable<object> Facts,

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

    [property: JsonPropertyName("sitemap")]
    IEnumerable<ITaxonomyTerm> Sitemap,

    [property: JsonPropertyName("url_pattern")]
    string UrlPattern
);