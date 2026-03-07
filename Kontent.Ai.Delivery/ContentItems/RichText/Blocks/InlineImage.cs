using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

/// <inheritdoc cref="IInlineImage" />
[DebuggerDisplay("Url = {" + nameof(Url) + "}")]
internal sealed record InlineImage : IInlineImage
{
    /// <inheritdoc/>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("height")]
    public int Height { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("width")]
    public int Width { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("image_id")]
    public Guid ImageId { get; init; }
}
