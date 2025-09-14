using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Fluent builder for listing content items.
/// </summary>
/// <typeparam name="TModel">Strongly typed elements model of the content items.</typeparam>
public interface IItemsQuery<TModel>
    where TModel : IElementsModel
{
    /// <summary>
    /// Sets the language codename for the request.
    /// </summary>
    /// <param name="languageCodename">Language codename.</param>
    /// <returns>The query builder for method chaining.</returns>
    IItemsQuery<TModel> WithLanguage(string languageCodename);

    /// <summary>
    /// Includes only specified element codenames in the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to include.</param>
    /// <returns>The query builder for method chaining.</returns>
    IItemsQuery<TModel> WithElements(params string[] elementCodenames);

    /// <summary>
    /// Excludes specified element codenames from the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to exclude.</param>
    /// <returns>The query builder for method chaining.</returns>
    IItemsQuery<TModel> WithoutElements(params string[] elementCodenames);

    /// <summary>
    /// Sets the linked items depth.
    /// </summary>
    /// <param name="depth">Depth value.</param>
    /// <returns>The query builder for method chaining.</returns>
    IItemsQuery<TModel> Depth(int depth);

    /// <summary>
    /// Sets the number of items to skip.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <returns>The query builder for method chaining.</returns>
    IItemsQuery<TModel> Skip(int skip);

    /// <summary>
    /// Sets the maximum number of items to return.
    /// </summary>
    /// <param name="limit">Maximum number of items.</param>
    /// <returns>The query builder for method chaining.</returns>
    IItemsQuery<TModel> Limit(int limit);

    /// <summary>
    /// Orders the items by the given path in ascending or descending order.
    /// </summary>
    /// <param name="elementOrAttributePath">Element or attribute path.</param>
    /// <param name="ascending">True for ascending; false for descending.</param>
    /// <returns>The query builder for method chaining.</returns>
    IItemsQuery<TModel> OrderBy(string elementOrAttributePath, bool ascending = true);

    /// <summary>
    /// Requests the total count to be included in the response.
    /// </summary>
    /// <returns>The query builder for method chaining.</returns>
    IItemsQuery<TModel> WithTotalCount();

    /// <summary>
    /// Overrides the global option for waiting on the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    /// <returns>The query builder for method chaining.</returns>
    IItemsQuery<TModel> WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Controls whether rich text elements are rendered into HTML strings for this request.
    /// Overrides the client-level default when specified.
    /// </summary>
    /// <param name="render">Whether to render rich text to HTML strings.</param>
    /// <returns>The query builder for method chaining.</returns>
    IItemsQuery<TModel> RenderRichTextToHtml(bool render = true);

    /// <summary>
    /// Adds a filter to the query using a filter builder function.
    /// </summary>
    /// <param name="filterBuilder">Function that builds a filter using the items filter builder.</param>
    /// <returns>The query builder for method chaining.</returns>
    IItemsQuery<TModel> Filter(Func<IItemFilters, IFilter> filterBuilder);

    /// <summary>
    /// Adds a filter to the query.
    /// </summary>
    /// <param name="filter">The filter to add.</param>
    /// <returns>The query builder for method chaining.</returns>
    IItemsQuery<TModel> Where(IFilter filter);

    /// <summary>
    /// Executes the built query and returns a functional result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A delivery result containing the content items or errors.</returns>
    Task<IDeliveryResult<IReadOnlyList<IContentItem<TModel>>>> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the query and retrieves all items by paging under the hood.
    /// Use with care on large environments due to latency/memory.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A delivery result containing all matching items or errors.</returns>
    Task<IDeliveryResult<IReadOnlyList<IContentItem<TModel>>>> ExecuteAllAsync(CancellationToken cancellationToken = default);
}
