using System.Collections.Generic;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes;

public record HeroUnit : IElementsModel
{
    [JsonPropertyName("image")]
    public IEnumerable<Asset> Image { get; init; }

    [JsonPropertyName("marketing_message")]
    public string MarketingMessage { get; init; }

    [JsonPropertyName("sitemap")]
    public IEnumerable<TaxonomyTerm> Sitemap { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; }
}