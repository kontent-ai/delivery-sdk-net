using System.Collections.Generic;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Rx.Tests.Models.ContentTypes;
public record Grinder
(
    [property: JsonPropertyName("image")]
    IEnumerable<IAsset> Image,

    [property: JsonPropertyName("long_description")]
    string LongDescription,

    [property: JsonPropertyName("manufacturer")]
    string Manufacturer,

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

    [property: JsonPropertyName("price")]
    decimal? Price,

    [property: JsonPropertyName("product_name")]
    string ProductName,

    [property: JsonPropertyName("product_status")]
    IEnumerable<ITaxonomyTerm> ProductStatus,

    [property: JsonPropertyName("short_description")]
    string ShortDescription,

    [property: JsonPropertyName("sitemap")]
    IEnumerable<ITaxonomyTerm> Sitemap,

    [property: JsonPropertyName("url_pattern")]
    string UrlPattern
);