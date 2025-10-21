using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Fluent builder for enumerating content items.
/// </summary>
/// <typeparam name="TModel">Strongly typed elements model of the content items.</typeparam>
public interface IEnumerateItemsQuery<TModel>
    where TModel : IElementsModel
{
    /// <summary>
    /// Sets the language codename for the request.
    /// </summary>
    /// <param name="languageCodename">Language codename.</param>
    IEnumerateItemsQuery<TModel> WithLanguage(string languageCodename);
    /// <summary>
    /// Includes only specified element codenames in the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to include.</param>
    IEnumerateItemsQuery<TModel> WithElements(params string[] elementCodenames);
    /// <summary>
    /// Orders the items by the given path in ascending or descending order.
    /// </summary>
    /// <param name="elementOrAttributePath">Element or attribute path.</param>
    /// <param name="ascending">True for ascending; false for descending.</param>
    IEnumerateItemsQuery<TModel> OrderBy(string elementOrAttributePath, bool ascending = true);

    /// <summary>
    /// Overrides the global option for waiting on the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    IEnumerateItemsQuery<TModel> WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Adds a filter to the query using a filter builder function.
    /// </summary>
    /// <param name="filterBuilder">Function that builds a filter using the items filter builder.</param>
    /// <returns>The query builder for method chaining.</returns>
    IEnumerateItemsQuery<TModel> Filter(Func<IItemFilters, IFilter> filterBuilder);

    /// <summary>
    /// Adds a filter to the query.
    /// </summary>
    /// <param name="filter">The filter to add.</param>
    /// <returns>The query builder for method chaining.</returns>
    IEnumerateItemsQuery<TModel> Where(IFilter filter);

    /// <summary>
    /// Enumerates content items using the Delivery API items-feed endpoint.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop enumeration and cancel in-flight requests.</param>
    /// <returns>Async sequence of strongly typed content items.</returns>
    IAsyncEnumerable<IContentItem<TModel>> EnumerateItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience method that enumerates all items and returns them as a list.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop enumeration and cancel in-flight requests.</param>
    /// <returns>All items aggregated into a read-only list.</returns>
    Task<IReadOnlyList<IContentItem<TModel>>> EnumerateAllAsync(CancellationToken cancellationToken = default);
}