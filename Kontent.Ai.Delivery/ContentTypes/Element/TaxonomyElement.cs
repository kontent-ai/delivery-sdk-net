using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentTypes.Element;

/// <summary>
/// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
/// </summary>
internal sealed record TaxonomyElement() : ContentElement, ITaxonomyElement
{
    /// <inheritdoc/>
    [JsonPropertyName("taxonomy_group")]
    public required string TaxonomyGroup { get; init; }
}
