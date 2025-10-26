using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes;

public record HeroUnit : IElementsModel
{
    [JsonPropertyName("image")]
    public required IEnumerable<Asset> Image { get; init; }

    [JsonPropertyName("marketing_message")]
    public required string MarketingMessage { get; init; }

    [JsonPropertyName("sitemap")]
    public required IEnumerable<TaxonomyTerm> Sitemap { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }
}