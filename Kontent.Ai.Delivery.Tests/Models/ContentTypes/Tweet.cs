using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes;

public record Tweet : IElementsModel
{
    [JsonPropertyName("display_options")]
    public IEnumerable<MultipleChoiceOption> DisplayOptions { get; init; }

    [JsonPropertyName("theme")]
    public IEnumerable<MultipleChoiceOption> Theme { get; init; }

    [JsonPropertyName("tweet_link")]
    public string TweetLink { get; init; }
}