using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.SyncV2;

/// <inheritdoc/>
internal sealed class SyncV2ContentTypeData : ISyncV2ContentTypeData
{
    /// <inheritdoc/>
    [JsonProperty("system")]
    public IContentTypeSystemAttributes System { get; internal set; }

    /// <summary>
    /// Constructor used for deserialization. Contains no logic.
    /// </summary>
    [JsonConstructor]
    public SyncV2ContentTypeData()
    {
    }
}
