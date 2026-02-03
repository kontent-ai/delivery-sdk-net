using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems;

/// <inheritdoc/>
public sealed record AssetRendition : IAssetRendition
{
    /// <inheritdoc/>
    [JsonPropertyName("rendition_id")]
    public required string RenditionId { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("preset_id")]
    public required string PresetId { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("width")]
    public required int Width { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("height")]
    public required int Height { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("query")]
    public required string Query { get; init; }
}
