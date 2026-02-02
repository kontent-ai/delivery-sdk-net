using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.SharedModels;

/// <inheritdoc/>
public sealed record DateTimeContent() : IDateTimeContent
{
    /// <inheritdoc/>
    [JsonPropertyName("value")]
    public DateTime? Value { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("display_timezone")]
    public string? DisplayTimezone { get; init; }
}