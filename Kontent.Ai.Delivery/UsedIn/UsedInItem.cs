using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.UsedIn;

/// <inheritdoc/>
internal sealed class UsedInItem : IUsedInItem
{
    /// <inheritdoc/>
    [JsonPropertyName("system")]
    public IUsedInItemSystemAttributes System { get; internal set; }

    /// <summary>
    /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
    /// </summary>
    [JsonConstructor]
    public UsedInItem(IUsedInItemSystemAttributes system)
    {
        System = system;
    }
}