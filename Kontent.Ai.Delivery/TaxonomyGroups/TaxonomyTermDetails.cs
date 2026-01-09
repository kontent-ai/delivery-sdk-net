using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.TaxonomyGroups;

[DebuggerDisplay("Name = {" + nameof(Name) + "}")]
internal sealed record TaxonomyTermDetails() : ITaxonomyTermDetails
{
    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("codename")]
    public required string Codename { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("terms")]
    public required IReadOnlyList<TaxonomyTermDetails> Terms { get; init; }

    IReadOnlyList<ITaxonomyTermDetails> ITaxonomyTermDetails.Terms => Terms;
}