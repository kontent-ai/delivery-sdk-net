using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.UsedIn;

/// <inheritdoc/>
/// <summary>
/// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
/// </summary>
[method: JsonConstructor]
/// <inheritdoc/>
internal sealed class UsedInItem(IUsedInItemSystemAttributes system) : IUsedInItem
{
    /// <inheritdoc/>
    [JsonPropertyName("system")]
    public IUsedInItemSystemAttributes System { get; internal set; } = system;
}