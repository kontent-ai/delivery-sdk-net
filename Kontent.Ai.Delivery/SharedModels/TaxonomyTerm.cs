using System.Text.Json.Serialization;
using System.Diagnostics;

namespace Kontent.Ai.Delivery.SharedModels;

/// <inheritdoc/>
/// <summary>
/// Initializes a new instance of the <see cref="TaxonomyTerm"/> class.
/// </summary>
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
