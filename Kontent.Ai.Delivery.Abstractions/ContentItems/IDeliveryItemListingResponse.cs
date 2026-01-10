namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from Kontent.ai Delivery API that contains a list of content items.
/// </summary>
/// <typeparam name="TModel">The type of content items in the response.</typeparam>
public interface IDeliveryItemListingResponse<TModel> : IPageable
{
    /// <summary>
    /// Gets a read-only list of content items.
    /// </summary>
    IReadOnlyList<IContentItem<TModel>> Items { get; }

    /// <summary>
    /// Gets a value indicating whether there are more pages available.
    /// </summary>
    bool HasNextPage { get; }

    /// <summary>
    /// Fetches the next page of results if available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The next page of results, or null if there are no more pages.</returns>
    Task<IDeliveryResult<IDeliveryItemListingResponse<TModel>>?> FetchNextPageAsync(CancellationToken cancellationToken = default);
}