using System.Text.Json.Serialization;
using System.Diagnostics;

namespace Kontent.Ai.Delivery.SharedModels;

/// <inheritdoc cref="IPagination" />
/// <summary>
/// Initializes a new instance of the <see cref="Pagination"/> class with information from a response.
/// </summary>
[DebuggerDisplay("Count = {" + nameof(Count) + "}, Total = {" + nameof(TotalCount) + "}")]
[method: JsonConstructor]
internal sealed class Pagination(int skip, int limit, int count, int? total_count, string next_page) : IPagination
{
    /// <inheritdoc/>
    public int Skip { get; } = skip;

    /// <inheritdoc/>
    public int Limit { get; } = limit;

    /// <inheritdoc/>
    public int Count { get; } = count;

    /// <inheritdoc/>
    [JsonPropertyName("total_count")]
    public int? TotalCount { get; } = total_count;

    /// <inheritdoc/>
    [JsonPropertyName("next_page")]
    public string? NextPageUrl { get; } = string.IsNullOrEmpty(next_page) ? null : next_page; // Normalize deserialization
}
