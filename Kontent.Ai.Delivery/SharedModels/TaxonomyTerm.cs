using System.Text.Json.Serialization;
using System.Diagnostics;

namespace Kontent.Ai.Delivery.SharedModels;

/// <inheritdoc/>
/// <summary>
/// Initializes a new instance of the <see cref="TaxonomyTerm"/> class.
/// </summary>
[DebuggerDisplay("Name = {" + nameof(Name) + "}")]
[method: JsonConstructor]
public sealed class TaxonomyTerm() : ITaxonomyTerm
{
    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public string Name { get; internal set; } // TODO: improve and consider changing to record

    /// <inheritdoc/>
    [JsonPropertyName("codename")]
    public string Codename { get; internal set; }
}
