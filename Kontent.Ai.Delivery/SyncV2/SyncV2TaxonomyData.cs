using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.SyncV2;

/// <inheritdoc/>
internal sealed class SyncV2TaxonomyData : ISyncV2TaxonomyData
{
    /// <inheritdoc/>
    [JsonPropertyName("system")]
    public ITaxonomyGroupSystemAttributes System { get; internal set; }

    /// <summary>
    /// Constructor used for deserialization. Contains no logic.
    /// </summary>
    [JsonConstructor]
    public SyncV2TaxonomyData()
    {
    }
}
