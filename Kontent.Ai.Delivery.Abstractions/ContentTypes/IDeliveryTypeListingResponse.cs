namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from Kontent.ai Delivery API that contains a list of content types.
/// </summary>
public interface IDeliveryTypeListingResponse : IPageable
{
    /// <summary>
    /// Gets a read-only list of content types.
    /// </summary>
    IReadOnlyList<IContentType> Types { get; }

    /// <summary>
    /// Gets a value indicating whether there are more types to fetch.
    /// </summary>
    bool HasNextPage { get; }

    /// <summary>
    /// Fetches the next page of types if available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next page of types, or null if no more pages exist.</returns>
    Task<IDeliveryResult<IDeliveryTypeListingResponse>?> FetchNextPageAsync(CancellationToken cancellationToken = default);
}