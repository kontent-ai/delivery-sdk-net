using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.SyncV2;

/// <inheritdoc/>
internal sealed class SyncV2Language : ISyncV2Language
{
    /// <inheritdoc/>
    [JsonPropertyName("data")]
    public ISyncV2LanguageData Data { get; }

    /// <inheritdoc/>
    [JsonPropertyName("change_type")]
    public string ChangeType { get; }

    /// <inheritdoc/>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SyncV2Language"/> class.
    /// </summary>
    [JsonConstructor]
    public SyncV2Language(ISyncV2LanguageData data, string changeType, DateTime timestamp)
    {
        Data = data;
        ChangeType = changeType;
        Timestamp = timestamp;
    }
}
