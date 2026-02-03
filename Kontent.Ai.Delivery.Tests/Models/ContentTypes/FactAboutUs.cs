using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Attributes;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes;

[ContentTypeCodename("fact_about_us")]
public record FactAboutUs
{
    [JsonPropertyName("description")]
    public required string Description { get; init; }
    [JsonPropertyName("image")]
    public required IEnumerable<Asset> Image { get; init; }
    [JsonPropertyName("sitemap")]
    public required IEnumerable<TaxonomyTerm> Sitemap { get; init; }
    [JsonPropertyName("title")]
    public required string Title { get; init; }
}
