using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentTypes.Element;

/// <inheritdoc/>
/// <summary>
/// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
/// </summary>
[DebuggerDisplay("Name = {" + nameof(Name) + "}")]
internal record ContentElement() : IContentElement
{
    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <inheritdoc/>
    /// <remarks>
    /// When deserializing from ContentType.Elements dictionary, this is hydrated from the dictionary key.
    /// When deserializing from direct element query, this comes from the JSON property.
    /// </remarks>
    [JsonPropertyName("codename")]
    public string Codename { get; init; } = string.Empty;
}