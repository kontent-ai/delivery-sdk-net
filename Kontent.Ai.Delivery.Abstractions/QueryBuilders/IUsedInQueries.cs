using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions.SharedModels;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders;

/// <summary>
/// Fluent builder for retrieving content items that use the specified item.
/// </summary>
public interface IItemUsedInQuery
{
    /// <summary>
    /// Overrides the global option for waiting on the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    IItemUsedInQuery WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Enumerates parent content items using the Used In endpoint.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop enumeration and cancel in-flight requests.</param>
    /// <returns>Async sequence of used-in items.</returns>
    IAsyncEnumerable<IUsedInItem> EnumerateItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience method that enumerates all used-in items and returns them as a list.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All used-in items aggregated into a read-only list.</returns>
    Task<IReadOnlyList<IUsedInItem>> EnumerateAllItemsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Fluent builder for retrieving content items that use the specified asset.
/// </summary>
public interface IAssetUsedInQuery
{
    /// <summary>
    /// Overrides the global option for waiting on the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    IAssetUsedInQuery WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Enumerates parent content items using the Asset Used In endpoint.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop enumeration and cancel in-flight requests.</param>
    /// <returns>Async sequence of used-in items.</returns>
    IAsyncEnumerable<IUsedInItem> EnumerateItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience method that enumerates all used-in items and returns them as a list.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All used-in items aggregated into a read-only list.</returns>
    Task<IReadOnlyList<IUsedInItem>> EnumerateAllItemsAsync(CancellationToken cancellationToken = default);
}
