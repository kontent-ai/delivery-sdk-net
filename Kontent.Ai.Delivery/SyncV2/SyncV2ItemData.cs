using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.SyncV2;

internal sealed class SyncV2ItemData : ISyncV2ItemData
{
    /// <inheritdoc/>
    [JsonPropertyName("system")]
    public IContentItemSystemAttributes System { get; internal set; }

    /// <summary>
    /// Constructor used for deserialization. Contains no logic.
    /// </summary>
    [JsonConstructor]
    public SyncV2ItemData()
    {
    }
}
