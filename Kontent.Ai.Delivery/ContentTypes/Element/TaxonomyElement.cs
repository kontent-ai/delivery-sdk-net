using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentTypes.Element;

/// <inheritdoc cref="ITaxonomyElement"/>
internal sealed record TaxonomyElement : ContentElement, ITaxonomyElement
{
    /// <inheritdoc/>
    [JsonPropertyName("taxonomy_group")]
    public required string TaxonomyGroup { get; init; }
}
