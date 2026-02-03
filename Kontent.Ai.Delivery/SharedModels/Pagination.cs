using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.SharedModels;

/// <inheritdoc cref="IPagination" />
/// <summary>
/// Initializes a new instance of the <see cref="Pagination"/> class with information from a response.
/// </summary>
[DebuggerDisplay("Count = {" + nameof(Count) + "}, Total = {" + nameof(TotalCount) + "}")]
internal sealed record Pagination : IPagination
{
    /// <inheritdoc/>
    [JsonPropertyName("skip")]
    public required int Skip { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("limit")]
    public required int Limit { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("count")]
    public required int Count { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("total_count")]
    public int? TotalCount { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("next_page")]
    public required string NextPageUrl { get; init; }
}
