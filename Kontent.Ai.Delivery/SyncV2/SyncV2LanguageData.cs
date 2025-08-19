using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.SyncV2;

/// <inheritdoc/>
internal sealed class SyncV2LanguageData : ISyncV2LanguageData
{
    /// <inheritdoc/>
    [JsonPropertyName("system")]
    public ILanguageSystemAttributes System { get; internal set; }

    /// <summary>
    /// Constructor used for deserialization. Contains no logic.
    /// </summary>
    [JsonConstructor]
    public SyncV2LanguageData()
    {
    }
}
