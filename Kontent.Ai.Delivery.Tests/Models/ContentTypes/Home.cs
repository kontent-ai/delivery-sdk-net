using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Attributes;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes;

[ContentTypeCodename("home")]
public record Home
{
    [JsonPropertyName("articles")]
    public required IEnumerable<IEmbeddedContent> Articles { get; init; }

    [JsonPropertyName("cafes")]
    public required IEnumerable<IEmbeddedContent> Cafes { get; init; }

    [JsonPropertyName("contact")]
    public required string Contact { get; init; }

    [JsonPropertyName("hero_unit")]
    public required IEnumerable<IEmbeddedContent> HeroUnit { get; init; }

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

    [JsonPropertyName("our_story")]
    public required IEnumerable<IEmbeddedContent> OurStory { get; init; }

    [JsonPropertyName("sitemap")]
    public required IEnumerable<TaxonomyTerm> Sitemap { get; init; }

    [JsonPropertyName("url_pattern")]
    public required string UrlPattern { get; init; }
}