namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from Kontent.ai Delivery API that contains a list of taxonomy groups.
/// </summary>
public interface IDeliveryTaxonomyListingResponse : IPageable
{
    /// <summary>
    /// Gets a read-only list of taxonomy groups.
    /// </summary>
    IReadOnlyList<ITaxonomyGroup> Taxonomies { get; }

    /// <summary>
    /// Gets a value indicating whether there are more taxonomy groups to fetch.
    /// </summary>
    bool HasNextPage { get; }

    /// <summary>
    /// Fetches the next page of taxonomy groups if available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next page of taxonomy groups, or null if no more pages exist.</returns>
    Task<IDeliveryResult<IDeliveryTaxonomyListingResponse>?> FetchNextPageAsync(CancellationToken cancellationToken = default);
}