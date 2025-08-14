using System;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders;

/// <summary>
/// Fluent builder for listing content items.
/// </summary>
public interface IMultipleItemsQuery<T>
{
    /// <summary>
    /// Sets the language codename for the request.
    /// </summary>
    /// <param name="languageCodename">Language codename.</param>
    IMultipleItemsQuery<T> WithLanguage(string languageCodename);
    /// <summary>
    /// Includes only specified element codenames in the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to include.</param>
    IMultipleItemsQuery<T> WithElements(params string[] elementCodenames);
    /// <summary>
    /// Excludes specified element codenames from the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to exclude.</param>
    IMultipleItemsQuery<T> WithoutElements(params string[] elementCodenames);
    /// <summary>
    /// Sets the linked items depth.
    /// </summary>
    /// <param name="depth">Depth value.</param>
    IMultipleItemsQuery<T> Depth(int depth);
    /// <summary>
    /// Sets the number of items to skip.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    IMultipleItemsQuery<T> Skip(int skip);
    /// <summary>
    /// Sets the maximum number of items to return.
    /// </summary>
    /// <param name="limit">Maximum number of items.</param>
    IMultipleItemsQuery<T> Limit(int limit);
    /// <summary>
    /// Orders the items by the given path in ascending or descending order.
    /// </summary>
    /// <param name="elementOrAttributePath">Element or attribute path.</param>
    /// <param name="ascending">True for ascending; false for descending.</param>
    IMultipleItemsQuery<T> OrderBy(string elementOrAttributePath, bool ascending = true);
    /// <summary>
    /// Requests the total count to be included in the response.
    /// </summary>
    IMultipleItemsQuery<T> WithTotalCount();

    /// <summary>
    /// Adds a filter to the query using a filter builder function.
    /// </summary>
    /// <param name="filterBuilder">Function that builds a filter using the items filter builder.</param>
    IMultipleItemsQuery<T> Filter(Func<IItemFilters, IFilter> filterBuilder);

    /// <summary>
    /// Adds a filter to the query.
    /// </summary>
    /// <param name="filter">The filter to add.</param>
    IMultipleItemsQuery<T> Where(IFilter filter);

    /// <summary>
    /// Executes the built query.
    /// </summary>
    /// <returns>Delivery items listing response.</returns>
    Task<IDeliveryItemListingResponse<T>> ExecuteAsync();
}
