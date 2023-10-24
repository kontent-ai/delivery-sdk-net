using System;
using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kontent.Ai.Delivery.Sync;

/// <inheritdoc/>
internal sealed class SyncItem : ISyncItem
{
    /// <inheritdoc/>
    [JsonProperty("data")]
    public object Data { get; internal set; }

    /// <inheritdoc/>
    [JsonProperty("change_type")]
    public string ChangeType { get; internal set; }

    /// <inheritdoc/>
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; internal set; }

    /// <summary>
    /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
    /// </summary>
    [JsonConstructor]
    public SyncItem(object data, string changeType, DateTime timestamp) {
        Data = data;
        ChangeType = changeType;
        Timestamp = timestamp;
    }
}