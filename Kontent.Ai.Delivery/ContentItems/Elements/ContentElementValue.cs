using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems.Elements;

internal class ContentElementValue<T> : IContentElementValue<T>
{
    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public required string Type { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("value")]
    public required T Value { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public required string Name { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("codename")]
    public required string Codename { get; internal set; }
}
