using System.Collections.Generic;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes;

public record Grinder : IElementsModel
{
    [JsonPropertyName("image")]
    public required IEnumerable<Asset> Image { get; init; }

    [JsonPropertyName("long_description")]
    public required string LongDescription { get; init; }

    [JsonPropertyName("manufacturer")]
    public required string Manufacturer { get; init; }

    [JsonPropertyName("metadata__meta_description")]
    public required string MetadataMetaDescription { get; init; }

    [JsonPropertyName("metadata__meta_title")]
    public required string MetadataMetaTitle { get; init; }

    [JsonPropertyName("metadata__og_description")]
    public required string MetadataOgDescription { get; init; }

    [JsonPropertyName("metadata__og_image")]
    public required IEnumerable<Asset> MetadataOgImage { get; init; }

    [JsonPropertyName("metadata__og_title")]
    public required string MetadataOgTitle { get; init; }

    [JsonPropertyName("metadata__twitter_creator")]
    public required string MetadataTwitterCreator { get; init; }

    [JsonPropertyName("metadata__twitter_description")]
    public required string MetadataTwitterDescription { get; init; }

    [JsonPropertyName("metadata__twitter_image")]
    public required IEnumerable<Asset> MetadataTwitterImage { get; init; }

    [JsonPropertyName("metadata__twitter_site")]
    public required string MetadataTwitterSite { get; init; }

    [JsonPropertyName("metadata__twitter_title")]
    public required string MetadataTwitterTitle { get; init; }

    [JsonPropertyName("price")]
    public decimal? Price { get; init; }

    [JsonPropertyName("product_name")]
    public required string ProductName { get; init; }

    [JsonPropertyName("product_status")]
    public required IEnumerable<TaxonomyTerm> ProductStatus { get; init; }

    [JsonPropertyName("short_description")]
    public required string ShortDescription { get; init; }

    [JsonPropertyName("sitemap")]
    public required IEnumerable<TaxonomyTerm> Sitemap { get; init; }

    [JsonPropertyName("url_pattern")]
    public required string UrlPattern { get; init; }
}