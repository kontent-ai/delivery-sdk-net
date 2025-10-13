using System.Collections.Generic;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes;

public record Tweet : IElementsModel
{
    [JsonPropertyName("display_options")]
    public IEnumerable<IMultipleChoiceOption> DisplayOptions { get; init; }

    [JsonPropertyName("theme")]
    public IEnumerable<IMultipleChoiceOption> Theme { get; init; }

    [JsonPropertyName("tweet_link")]
    public string TweetLink { get; init; }
}