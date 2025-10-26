using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems.DateTimes;

internal sealed record DateTimeContent() : IDateTimeContent
{
    [JsonPropertyName("value")]
    public DateTime? Value { get; init; }

    [JsonPropertyName("display_timezone")]
    public string? DisplayTimezone { get; init; }
}