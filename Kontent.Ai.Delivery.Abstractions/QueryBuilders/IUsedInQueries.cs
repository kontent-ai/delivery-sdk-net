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
    /// Adds filtering conditions to the query.
    /// </summary>
    /// <remarks>
    /// The returned query uses AND semantics between conditions (multiple query parameters).
    /// </remarks>
    /// <param name="build">Builder function that appends one or more filtering conditions.</param>
    /// <returns>The query builder for method chaining.</returns>
    IItemUsedInQuery Where(Func<IItemsFilterBuilder, IItemsFilterBuilder> build);

    /// <summary>
    /// Enumerates parent content items using the Used In endpoint.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop enumeration and cancel in-flight requests.</param>
    /// <returns>
    /// Async sequence of used-in items.
    /// Enumeration stops when a page request fails and returns items already received.
    /// Use SDK extension methods for status-aware page enumeration.
    /// </returns>
    IAsyncEnumerable<IUsedInItem> EnumerateAsync(CancellationToken cancellationToken = default);
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
    /// Adds filtering conditions to the query.
    /// </summary>
    /// <remarks>
    /// The returned query uses AND semantics between conditions (multiple query parameters).
    /// </remarks>
    /// <param name="build">Builder function that appends one or more filtering conditions.</param>
    /// <returns>The query builder for method chaining.</returns>
    IAssetUsedInQuery Where(Func<IItemsFilterBuilder, IItemsFilterBuilder> build);

    /// <summary>
    /// Enumerates parent content items using the Asset Used In endpoint.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop enumeration and cancel in-flight requests.</param>
    /// <returns>
    /// Async sequence of used-in items.
    /// Enumeration stops when a page request fails and returns items already received.
    /// Use SDK extension methods for status-aware page enumeration.
    /// </returns>
    IAsyncEnumerable<IUsedInItem> EnumerateAsync(CancellationToken cancellationToken = default);
}
