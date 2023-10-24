using System;
using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kontent.Ai.Delivery.Sync;

/// <inheritdoc/>
internal sealed class SyncItem : ISyncItem
{
    [JsonProperty("data")]
    public ISyncItemData Data { get; internal set; }

    [JsonProperty("change_type")]
    public string ChangeType { get; internal set; }

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; internal set; }

    [JsonConstructor]
    public SyncItem(ISyncItemData data, string changeType, DateTime timestamp) {
        Data = data;
        ChangeType = changeType;
        Timestamp = timestamp;
    }
}