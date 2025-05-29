using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.SyncV2;

internal sealed class SyncV2ItemData : ISyncV2ItemData
{
    /// <inheritdoc/>
    [JsonProperty("system")]
    public IContentItemSystemAttributes System { get; internal set; }

    /// <summary>
    /// Constructor used for deserialization. Contains no logic.
    /// </summary>
    [JsonConstructor]
    public SyncV2ItemData()
    {
    }
}
