using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.SharedModels;

/// <inheritdoc cref="IError" />
internal sealed record Error : IError
{
    /// <inheritdoc/>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("request_id")]
    public string? RequestId { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("error_code")]
    public int? ErrorCode { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("specific_code")]
    public int? SpecificCode { get; init; }
}
