using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.TaxonomyGroups;

/// <inheritdoc/>
/// <summary>
/// Initializes a new instance of the <see cref="TaxonomyGroupSystemAttributes"/> class.
/// </summary>
[DebuggerDisplay("Id = {" + nameof(Id) + "}")]
[method: JsonConstructor]
internal sealed record TaxonomyGroupSystemAttributes() : ITaxonomyGroupSystemAttributes
{
    /// <inheritdoc/>
    [JsonPropertyName("id")]
    public string Id { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public string Name { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("codename")]
    public string Codename { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("last_modified")]
    public DateTime LastModified { get; init; }
}