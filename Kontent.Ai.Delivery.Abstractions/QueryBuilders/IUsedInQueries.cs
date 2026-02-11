namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Fluent builder for retrieving content items that use the specified item.
/// </summary>
public interface IItemUsedInQuery
{
    /// <summary>
    /// Configures waiting for the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    IItemUsedInQuery WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Enumerates parent content items using the Used In endpoint.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop enumeration and cancel in-flight requests.</param>
    /// <returns>
    /// Async sequence of used-in items.
    /// Enumeration stops when a page request fails and returns items already received.
    /// Use SDK extension methods for status-aware page enumeration.
    /// </returns>
    IAsyncEnumerable<IUsedInItem> EnumerateItemsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Fluent builder for retrieving content items that use the specified asset.
/// </summary>
public interface IAssetUsedInQuery
{
    /// <summary>
    /// Configures waiting for the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    IAssetUsedInQuery WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Enumerates parent content items using the Asset Used In endpoint.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop enumeration and cancel in-flight requests.</param>
    /// <returns>
    /// Async sequence of used-in items.
    /// Enumeration stops when a page request fails and returns items already received.
    /// Use SDK extension methods for status-aware page enumeration.
    /// </returns>
    IAsyncEnumerable<IUsedInItem> EnumerateItemsAsync(CancellationToken cancellationToken = default);
}
