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

    /// <summary>
    /// Delegate that fetches the next page when invoked. Injected by the query builder.
    /// </summary>
    [JsonIgnore]
    internal Func<CancellationToken, Task<IDeliveryResult<IDeliveryTaxonomyListingResponse>>>? NextPageFetcher { get; init; }

    IReadOnlyList<ITaxonomyGroup> IDeliveryTaxonomyListingResponse.Taxonomies => Taxonomies;
    IPagination IPageable.Pagination => Pagination;

    /// <inheritdoc/>
    public bool HasNextPage => !string.IsNullOrEmpty(Pagination.NextPageUrl);

    /// <inheritdoc/>
    public async Task<IDeliveryResult<IDeliveryTaxonomyListingResponse>?> FetchNextPageAsync(CancellationToken cancellationToken = default)
    {
        return !HasNextPage || NextPageFetcher is null ? null : await NextPageFetcher(cancellationToken).ConfigureAwait(false);
    }
}
