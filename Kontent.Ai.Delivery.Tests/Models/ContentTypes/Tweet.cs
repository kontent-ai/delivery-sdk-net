using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Attributes;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes;

[ContentTypeCodename("tweet")]
public record Tweet
{
    [JsonPropertyName("display_options")]
    public IEnumerable<MultipleChoiceOption>? DisplayOptions { get; init; }

    [JsonPropertyName("theme")]
    public IEnumerable<MultipleChoiceOption>? Theme { get; init; }

    [JsonPropertyName("tweet_link")]
    public string? TweetLink { get; init; }
}