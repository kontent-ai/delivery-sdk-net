using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.SharedModels;

/// <inheritdoc cref="ITaxonomyTerm"/>
[DebuggerDisplay("Name = {" + nameof(Name) + "}")]
public sealed record TaxonomyTerm : ITaxonomyTerm
{
    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("codename")]
    public required string Codename { get; init; }
}
