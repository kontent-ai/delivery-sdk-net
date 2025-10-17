using System.Collections.Generic;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes;

public record Grinder : IElementsModel
{
    [JsonPropertyName("image")]
    public IEnumerable<Asset> Image { get; init; }

    [JsonPropertyName("long_description")]
    public string LongDescription { get; init; }

    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; init; }

    [JsonPropertyName("metadata__meta_description")]
    public string MetadataMetaDescription { get; init; }

    [JsonPropertyName("metadata__meta_title")]
    public string MetadataMetaTitle { get; init; }

    [JsonPropertyName("metadata__og_description")]
    public string MetadataOgDescription { get; init; }

    [JsonPropertyName("metadata__og_image")]
    public IEnumerable<Asset> MetadataOgImage { get; init; }

    [JsonPropertyName("metadata__og_title")]
    public string MetadataOgTitle { get; init; }

    [JsonPropertyName("metadata__twitter_creator")]
    public string MetadataTwitterCreator { get; init; }

    [JsonPropertyName("metadata__twitter_description")]
    public string MetadataTwitterDescription { get; init; }

    [JsonPropertyName("metadata__twitter_image")]
    public IEnumerable<Asset> MetadataTwitterImage { get; init; }

    [JsonPropertyName("metadata__twitter_site")]
    public string MetadataTwitterSite { get; init; }

    [JsonPropertyName("metadata__twitter_title")]
    public string MetadataTwitterTitle { get; init; }

    [JsonPropertyName("price")]
    public decimal? Price { get; init; }

    [JsonPropertyName("product_name")]
    public string ProductName { get; init; }

    [JsonPropertyName("product_status")]
    public IEnumerable<TaxonomyTerm> ProductStatus { get; init; }

    [JsonPropertyName("short_description")]
    public string ShortDescription { get; init; }

    [JsonPropertyName("sitemap")]
    public IEnumerable<TaxonomyTerm> Sitemap { get; init; }

    [JsonPropertyName("url_pattern")]
    public string UrlPattern { get; init; }
}