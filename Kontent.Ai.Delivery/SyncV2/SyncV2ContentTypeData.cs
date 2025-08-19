using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.SyncV2;

/// <inheritdoc/>
internal sealed class SyncV2ContentTypeData : ISyncV2ContentTypeData
{
    /// <inheritdoc/>
    [JsonPropertyName("system")]
    public IContentTypeSystemAttributes System { get; internal set; }

    /// <summary>
    /// Constructor used for deserialization. Contains no logic.
    /// </summary>
    [JsonConstructor]
    public SyncV2ContentTypeData()
    {
    }
}
