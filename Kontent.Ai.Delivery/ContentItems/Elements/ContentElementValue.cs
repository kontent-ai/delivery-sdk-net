using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems.Elements;

internal class ContentElementValue<T> : IContentElementValue<T>
{
    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("value")]
    public required T Value { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("codename")]
    public required string Codename { get; set; }
}
