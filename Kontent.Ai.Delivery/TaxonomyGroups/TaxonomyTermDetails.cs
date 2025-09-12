using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.TaxonomyGroups;

/// <inheritdoc/>
/// <summary>
/// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
/// </summary>
[DebuggerDisplay("Name = {" + nameof(Name) + "}")]
[method: JsonConstructor]
internal sealed class TaxonomyTermDetails() : ITaxonomyTermDetails
{
    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public string Name { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("codename")]
    public string Codename { get; internal set; }

    /// <inheritdoc/>
    [JsonPropertyName("terms")]
    public IList<ITaxonomyTermDetails> Terms { get; internal set; }
}
