using System;
using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kontent.Ai.Delivery.Sync;

/// <inheritdoc/>
/// <summary>
/// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
/// </summary>
[method: JsonConstructor]
/// <<inheritdoc/>>
internal sealed class SyncItem(object data, string changeType, DateTime timestamp) : ISyncItem
{
    /// <inheritdoc/>
    [JsonProperty("data")]
    public object Data { get; internal set; } = data;

    /// <inheritdoc/>
    [JsonProperty("change_type")]
    public string ChangeType { get; internal set; } = changeType;

    /// <inheritdoc/>
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; internal set; } = timestamp;
}