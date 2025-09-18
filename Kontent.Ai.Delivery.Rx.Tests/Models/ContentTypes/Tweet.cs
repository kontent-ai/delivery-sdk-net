using System.Collections.Generic;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Rx.Tests.Models.ContentTypes;
public record Tweet
(
    [property: JsonPropertyName("display_options")]
    IEnumerable<IMultipleChoiceOption> DisplayOptions,

    [property: JsonPropertyName("theme")]
    IEnumerable<IMultipleChoiceOption> Theme,

    [property: JsonPropertyName("tweet_link")]
    string TweetLink
);