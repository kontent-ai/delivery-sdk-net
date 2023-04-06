using System;
using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.Sync;

/// <inheritdoc/>
public class SyncItem : ISyncItem
{
    /// <inheritdoc/>
    [JsonProperty("codename")]
    public string Codename { get; internal set; }

    /// <inheritdoc/>
    [JsonProperty("id")]
    public Guid Id { get; internal set; }

    /// <inheritdoc/>
    [JsonProperty("type")]
    public string Type { get; internal set; }

    /// <inheritdoc/>
    [JsonProperty("language")]
    public string Language { get; internal set; }

    /// <inheritdoc/>
    [JsonProperty("collection")]
    public string Collection { get; internal set; }

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
    public SyncItem()
    {
    }
}