using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.SharedModels;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.Tests.Models;

public record Homepage : IElementsModel
{
    [JsonPropertyName("call_to_action")]
    public string CallToAction { get; init; }
    [JsonPropertyName("subtitle")]
    public string Subtitle { get; init; }
    [JsonPropertyName("image")]
    public IEnumerable<Asset> Image { get; init; }
    [JsonPropertyName("untitled_taxonomy_group")]
    public IEnumerable<TaxonomyTerm> UntitledTaxonomyGroup { get; init; }
    [JsonPropertyName("system")]
    public IContentItemSystemAttributes System { get; init; }
}