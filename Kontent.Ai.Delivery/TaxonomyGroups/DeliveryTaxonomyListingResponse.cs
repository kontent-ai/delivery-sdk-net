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
    public Pagination Pagination
    {
        get; init;
    }

    /// <summary>
    /// Gets a read-only list of taxonomy groups.
    /// </summary>
    [JsonPropertyName("taxonomies")]
    public IList<TaxonomyGroup> Taxonomies
    {
        get; init;
    }

    IList<ITaxonomyGroup> IDeliveryTaxonomyListingResponse.Taxonomies => Taxonomies.Cast<ITaxonomyGroup>().ToList();

    IPagination IPageable.Pagination => Pagination;
}
