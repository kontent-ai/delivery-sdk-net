using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes;

public record HostedVideo : IElementsModel
{
    [JsonPropertyName("video_host")]
    public IEnumerable<MultipleChoiceOption>? VideoHost { get; init; }

    [JsonPropertyName("video_id")]
    public string? VideoId { get; init; }
}