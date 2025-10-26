using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentTypes.Element;

/// <summary>
/// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
/// </summary>
[method: JsonConstructor]
internal sealed record TaxonomyElement() : ContentElement, ITaxonomyElement
{
    /// <inheritdoc/>
    [JsonPropertyName("taxonomy_group")]
    public string? TaxonomyGroup { get; init; }
}