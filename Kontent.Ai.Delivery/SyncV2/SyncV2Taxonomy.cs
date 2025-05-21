using System;
using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.SyncV2;

/// <inheritdoc/>
public class SyncV2Taxonomy : ISyncV2Taxonomy
{
    /// <inheritdoc/>
    [JsonProperty("data")]
    public ISyncV2TaxonomyData Data { get; }

    /// <inheritdoc/>
    [JsonProperty("change_type")]
    public string ChangeType { get; }

    /// <inheritdoc/>
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SyncV2Taxonomy"/> class.
    /// </summary>
    [JsonConstructor]
    public SyncV2Taxonomy(object stronglyTypedData, ISyncV2TaxonomyData data, string changeType, DateTime timestamp)
    {
        Data = data;
        ChangeType = changeType;
        Timestamp = timestamp;
    }
}
