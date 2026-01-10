namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a partial response from Kontent.ai Delivery API enumeration methods that contains a list of content items.
/// </summary>
/// <typeparam name="TModel">The type of content items in the response.</typeparam>
public interface IDeliveryItemsFeedResponse<TModel>
{
    /// <summary>
    /// Gets a read-only list of content items.
    /// </summary>
    IReadOnlyList<IContentItem<TModel>> Items { get; }

    /// <summary>
    /// Gets a value indicating whether there are more items to fetch.
    /// </summary>
    bool HasNextPage { get; }

    /// <summary>
    /// Fetches the next page of items if available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next page of items, or null if no more pages exist.</returns>
    Task<IDeliveryResult<IDeliveryItemsFeedResponse<TModel>>?> FetchNextPageAsync(CancellationToken cancellationToken = default);
}