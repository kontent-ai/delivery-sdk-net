using System.Collections.Generic;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Tests.Models;

public record ArticlePartialItemModel : IElementsModel
{
    [JsonPropertyName("title")]
    public string Title { get; init; }
    [JsonPropertyName("summary")]
    public string Summary { get; init; }
    [JsonPropertyName("personas")]
    public IEnumerable<TaxonomyTerm> Personas { get; init; }
    [JsonPropertyName("system")]
    public IContentItemSystemAttributes System { get; init; }
}