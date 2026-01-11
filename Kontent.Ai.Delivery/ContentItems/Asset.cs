using System.Text.Json.Serialization;
using System.Diagnostics;

namespace Kontent.Ai.Delivery.ContentItems;

/// <inheritdoc/>
[DebuggerDisplay("Name = {" + nameof(Name) + "}")]
public sealed record Asset : IAsset
{
    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("size")]
    public required int Size { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("width")]
    public int Width { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("height")]
    public int Height { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("renditions")]
    public required IReadOnlyDictionary<string, IAssetRendition> Renditions { get; init; }
}