using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.TaxonomyGroups;

/// <summary>
/// Represents a response from Kontent.ai Delivery API that contains a list of taxonomy groups.
/// </summary>
internal sealed record DeliveryTaxonomyListingResponse : IDeliveryTaxonomyListingResponse
{
    /// <summary>
    /// Gets paging information.
    /// </summary>
    [JsonPropertyName("pagination")]
    public required Pagination Pagination { get; init; }

    /// <summary>
    /// Gets a read-only list of taxonomy groups.
    /// </summary>
    [JsonPropertyName("taxonomies")]
    public required IReadOnlyList<TaxonomyGroup> Taxonomies { get; init; }

    IReadOnlyList<ITaxonomyGroup> IDeliveryTaxonomyListingResponse.Taxonomies => Taxonomies;
    IPagination IPageable.Pagination => Pagination;
}
