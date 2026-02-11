using System.Runtime.CompilerServices;
using Kontent.Ai.Delivery.Api.QueryBuilders;

namespace Kontent.Ai.Delivery;

/// <summary>
/// Extension methods for status-aware query enumeration.
/// </summary>
public static class QueryEnumerationExtensions
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

    /// <summary>
    /// Enumerates feed pages for a typed items feed query and includes the request status for each page.
    /// </summary>
    /// <typeparam name="TModel">The item model type.</typeparam>
    /// <param name="query">The typed items feed query.</param>
    /// <param name="cancellationToken">Cancellation token to stop enumeration and cancel in-flight requests.</param>
    /// <returns>
    /// Async sequence of page results.
    /// Successful pages contain feed responses in <see cref="IDeliveryResult{T}.Value"/>.
    /// When a page request fails, the sequence yields one failed result and then stops.
    /// </returns>
    public static IAsyncEnumerable<IDeliveryResult<IDeliveryItemsFeedResponse<TModel>>> EnumerateItemsWithStatusAsync<TModel>(
        this IEnumerateItemsQuery<TModel> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return EnumerateFeedPagesWithStatusAsync(
            query.ExecuteAsync,
            static page => page.HasNextPage,
            static (page, ct) => page.FetchNextPageAsync(ct),
            cancellationToken);
    }

    /// <summary>
    /// Enumerates feed pages for a dynamic items feed query and includes the request status for each page.
    /// </summary>
    /// <param name="query">The dynamic items feed query.</param>
    /// <param name="cancellationToken">Cancellation token to stop enumeration and cancel in-flight requests.</param>
    /// <returns>
    /// Async sequence of page results.
    /// Successful pages contain feed responses in <see cref="IDeliveryResult{T}.Value"/>.
    /// When a page request fails, the sequence yields one failed result and then stops.
    /// </returns>
    public static IAsyncEnumerable<IDeliveryResult<IDeliveryItemsFeedResponse>> EnumerateItemsWithStatusAsync(
        this IDynamicEnumerateItemsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return EnumerateFeedPagesWithStatusAsync(
            query.ExecuteAsync,
            static page => page.HasNextPage,
            static (page, ct) => page.FetchNextPageAsync(ct),
            cancellationToken);
    }

    private static async IAsyncEnumerable<IDeliveryResult<TPage>> EnumerateFeedPagesWithStatusAsync<TPage>(
        Func<CancellationToken, Task<IDeliveryResult<TPage>>> fetchFirstPageAsync,
        Func<TPage, bool> hasNextPage,
        Func<TPage, CancellationToken, Task<IDeliveryResult<TPage>?>> fetchNextPageAsync,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pageResult = await fetchFirstPageAsync(cancellationToken).ConfigureAwait(false);

        while (true)
        {
            yield return pageResult;

            if (!pageResult.IsSuccess || !hasNextPage(pageResult.Value))
            {
                yield break;
            }

            var nextPageResult = await fetchNextPageAsync(pageResult.Value, cancellationToken).ConfigureAwait(false);
            if (nextPageResult is null)
            {
                yield break;
            }

            pageResult = nextPageResult;
        }
    }

    private static IAsyncEnumerable<IDeliveryResult<IReadOnlyList<IUsedInItem>>> ThrowUnsupportedStatusEnumeration(string queryType)
        => throw new NotSupportedException(
            $"Query type '{queryType}' does not support status-aware used-in enumeration. Use the SDK-provided query builders from IDeliveryClient.");
}
