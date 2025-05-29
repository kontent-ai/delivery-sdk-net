using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;
using System;

namespace Kontent.Ai.Delivery.SyncV2;

/// <inheritdoc/>
internal sealed class SyncV2ContentType : ISyncV2ContentType
{
    /// <inheritdoc/>
    [JsonProperty("data")]
    public ISyncV2ContentTypeData Data { get; }

    /// <inheritdoc/>
    [JsonProperty("change_type")]
    public string ChangeType { get; }

    /// <inheritdoc/>
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SyncV2ContentType"/> class.
    /// </summary>
    [JsonConstructor]
    public SyncV2ContentType(ISyncV2ContentTypeData data, string changeType, DateTime timestamp)
    {
        Data = data;
        ChangeType = changeType;
        Timestamp = timestamp;
    }
}
