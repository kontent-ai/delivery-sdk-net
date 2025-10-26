using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.RichText;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes;

public record Article : IElementsModel
{
    [JsonPropertyName("body_copy")]
    public RichTextContent? BodyCopy { get; init; }

    [JsonPropertyName("metadata__meta_description")]
    public string? MetadataMetaDescription { get; init; }

    [JsonPropertyName("metadata__meta_title")]
    public string? MetadataMetaTitle { get; init; }

    [JsonPropertyName("metadata__og_description")]
    public string? MetadataOgDescription { get; init; }

    [JsonPropertyName("metadata__og_image")]
    public IEnumerable<Asset>? MetadataOgImage { get; init; }

    [JsonPropertyName("metadata__og_title")]
    public string? MetadataOgTitle { get; init; }

    [JsonPropertyName("metadata__twitter_creator")]
    public string? MetadataTwitterCreator { get; init; }

    [JsonPropertyName("metadata__twitter_description")]
    public string? MetadataTwitterDescription { get; init; }

    [JsonPropertyName("metadata__twitter_image")]
    public IEnumerable<Asset>? MetadataTwitterImage { get; init; }

    [JsonPropertyName("metadata__twitter_site")]
    public string? MetadataTwitterSite { get; init; }

    [JsonPropertyName("metadata__twitter_title")]
    public string? MetadataTwitterTitle { get; init; }

    [JsonPropertyName("meta_description")]
    public string? MetaDescription { get; init; }

    [JsonPropertyName("meta_keywords")]
    public string? MetaKeywords { get; init; }

    [JsonPropertyName("personas")]
    public IEnumerable<TaxonomyTerm>? Personas { get; init; }

    [JsonPropertyName("post_date")]
    public DateTime? PostDate { get; init; }

    [JsonPropertyName("related_articles")]
    public IEnumerable<IEmbeddedContent>? RelatedArticles { get; init; }

    [JsonPropertyName("sitemap")]
    public IEnumerable<TaxonomyTerm>? Sitemap { get; init; }

    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("teaser_image")]
    public IEnumerable<Asset>? TeaserImage { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("url_pattern")]
    public string? UrlPattern { get; init; }
}
