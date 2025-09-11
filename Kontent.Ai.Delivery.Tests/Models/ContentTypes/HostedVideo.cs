using System.Collections.Generic;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes;

public record HostedVideo
(
    [property: JsonPropertyName("video_host")]
    IEnumerable<IMultipleChoiceOption> VideoHost,

    [property: JsonPropertyName("video_id")]
    string VideoId
);