using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentTypes.Element;

/// <inheritdoc/>
/// <summary>
/// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
/// </summary>
[DebuggerDisplay("Name = {" + nameof(Name) + "}")]
[method: JsonConstructor]
internal class ContentElement() : IContentElement
{
    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public string Type { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public string Name { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("codename")]
    public string Codename { get; internal set; }
}
