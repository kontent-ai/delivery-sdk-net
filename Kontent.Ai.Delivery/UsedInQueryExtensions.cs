using Kontent.Ai.Delivery.Api.QueryBuilders;

namespace Kontent.Ai.Delivery;

/// <summary>
/// Extension methods for used-in queries.
/// </summary>
public static class UsedInQueryExtensions
{
    /// <summary>
    /// Enumerates used-in results page by page and includes the request status for each page.
    /// </summary>
    /// <param name="query">The item used-in query.</param>
    /// <param name="cancellationToken">Cancellation token to stop enumeration and cancel in-flight requests.</param>
    /// <returns>
    /// Async sequence of page results.
    /// Successful pages contain a list of used-in items in <see cref="IDeliveryResult{T}.Value"/>.
    /// When a page request fails, the sequence yields one failed result and then stops.
    /// </returns>
    public static IAsyncEnumerable<IDeliveryResult<IReadOnlyList<IUsedInItem>>> EnumerateItemsWithStatusAsync(
        this IItemUsedInQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return query is IUsedInQueryStatusProvider provider
            ? provider.EnumerateItemsWithStatusAsync(cancellationToken)
            : ThrowUnsupportedStatusEnumeration(nameof(IItemUsedInQuery));
    }

    /// <summary>
    /// Enumerates asset used-in results page by page and includes the request status for each page.
    /// </summary>
    /// <param name="query">The asset used-in query.</param>
    /// <param name="cancellationToken">Cancellation token to stop enumeration and cancel in-flight requests.</param>
    /// <returns>
    /// Async sequence of page results.
    /// Successful pages contain a list of used-in items in <see cref="IDeliveryResult{T}.Value"/>.
    /// When a page request fails, the sequence yields one failed result and then stops.
    /// </returns>
    public static IAsyncEnumerable<IDeliveryResult<IReadOnlyList<IUsedInItem>>> EnumerateItemsWithStatusAsync(
        this IAssetUsedInQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return query is IUsedInQueryStatusProvider provider
            ? provider.EnumerateItemsWithStatusAsync(cancellationToken)
            : ThrowUnsupportedStatusEnumeration(nameof(IAssetUsedInQuery));
    }

    private static IAsyncEnumerable<IDeliveryResult<IReadOnlyList<IUsedInItem>>> ThrowUnsupportedStatusEnumeration(string queryType)
        => throw new NotSupportedException(
            $"Query type '{queryType}' does not support status-aware used-in enumeration. Use the SDK-provided query builders from IDeliveryClient.");
}
