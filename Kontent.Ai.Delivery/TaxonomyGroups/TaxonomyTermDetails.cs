using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.TaxonomyGroups;

/// <inheritdoc/>
/// <summary>
/// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
/// </summary>
[DebuggerDisplay("Name = {" + nameof(Name) + "}")]
[method: JsonConstructor]
internal sealed record TaxonomyTermDetails() : ITaxonomyTermDetails
{
    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("codename")]
    public string? Codename { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("terms")]
    public IList<TaxonomyTermDetails>? Terms { get; init; }
    IList<ITaxonomyTermDetails> ITaxonomyTermDetails.Terms => [.. Terms.Cast<ITaxonomyTermDetails>()];
}