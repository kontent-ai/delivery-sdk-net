using System;
using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.SyncV2;

/// <inheritdoc/>
public class SyncV2Language : ISyncV2Language
{
    /// <inheritdoc/>
    [JsonProperty("data")]
    public ISyncV2LanguageData Data { get; }

    /// <inheritdoc/>
    [JsonProperty("change_type")]
    public string ChangeType { get; }

    /// <inheritdoc/>
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SyncV2Language"/> class.
    /// </summary>
    [JsonConstructor]
    public SyncV2Language(object stronglyTypedData, ISyncV2LanguageData data, string changeType, DateTime timestamp)
    {
        Data = data;
        ChangeType = changeType;
        Timestamp = timestamp;
    }
}
