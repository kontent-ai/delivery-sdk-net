using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.Sync;

internal sealed class SyncItemData : ISyncItemData
{
    /// <inheritdoc/>
    [JsonPropertyName("system")]
    public IContentItemSystemAttributes System { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("elements")]
    public Dictionary<string, object> Elements { get; internal set; }

    /// <summary>
    /// Constructor used for deserialization. Contains no logic.
    /// </summary>
    [JsonConstructor]
    public SyncItemData()
    {
    }
}
