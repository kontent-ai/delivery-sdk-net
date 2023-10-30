using System;
using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.Sync;

/// <inheritdoc/>
internal sealed class SyncItem : ISyncItem
{
    /// <inheritdoc/>
    public object StronglyTypedData { get; internal set; }

    /// <inheritdoc/>
    [JsonProperty("data")]
    public ISyncItemData Data { get; internal set; }

    /// <inheritdoc/>
    [JsonProperty("change_type")]
    public string ChangeType { get; internal set; }

    /// <inheritdoc/>
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; internal set; }

    /// <summary>
    /// Initializes a new instance of <see cref="SyncItem"/> class.
    /// </summary>
    [JsonConstructor]
    public SyncItem(object stronglyTypedData, ISyncItemData data, string changeType, DateTime timestamp)
    {
        StronglyTypedData = stronglyTypedData;
        Data = data;
        ChangeType = changeType;
        Timestamp = timestamp;
    }
}