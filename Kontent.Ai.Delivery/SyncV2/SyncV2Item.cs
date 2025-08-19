using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.SyncV2;

/// <inheritdoc/>
internal sealed class SyncV2Item : ISyncV2Item
{
    /// <inheritdoc/>
    [JsonPropertyName("data")]
    public ISyncV2ItemData Data { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("change_type")]
    public string ChangeType { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; internal set; }

    /// <summary>
    /// Initializes a new instance of <see cref="SyncV2Item"/> class.
    /// </summary>
    [JsonConstructor]
    public SyncV2Item(ISyncV2ItemData data, string changeType, DateTime timestamp)
    {
        Data = data;
        ChangeType = changeType;
        Timestamp = timestamp;
    }
}