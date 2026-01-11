using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.SharedModels;

public sealed record DateTimeContent() : IDateTimeContent
{
    [JsonPropertyName("value")]
    public DateTime? Value { get; init; }

    [JsonPropertyName("display_timezone")]
    public string? DisplayTimezone { get; init; }
}