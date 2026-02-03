using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.SharedModels;

/// <inheritdoc cref="IError" />
internal sealed record Error : IError
{
    /// <inheritdoc/>
    /// <remarks>
    /// Defaults to "Unknown error" to handle API responses that omit the message field.
    /// </remarks>
    [JsonPropertyName("message")]
    public string Message { get; init; } = "Unknown error";

    /// <inheritdoc/>
    [JsonPropertyName("request_id")]
    public string? RequestId { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("error_code")]
    public int? ErrorCode { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("specific_code")]
    public int? SpecificCode { get; init; }

    /// <inheritdoc/>
    /// <remarks>
    /// This property is not serialized to JSON as exceptions are not meant to be persisted.
    /// </remarks>
    [JsonIgnore]
    public Exception? Exception { get; init; }
}
