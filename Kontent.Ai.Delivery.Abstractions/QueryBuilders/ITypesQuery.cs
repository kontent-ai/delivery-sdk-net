using System;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders;

/// <summary>
/// Fluent builder for listing content types.
/// </summary>
public interface ITypesQuery
{
    /// <summary>
    /// Includes only specified element codenames in the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to include.</param>
    ITypesQuery WithElements(params string[] elementCodenames);

    /// <summary>
    /// Sets the number of types to skip.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    ITypesQuery Skip(int skip);

    /// <summary>
    /// Sets the maximum number of types to return.
    /// </summary>
    /// <param name="limit">Maximum number of items.</param>
    ITypesQuery Limit(int limit);

    /// <summary>
    /// Adds a filter to the query using a filter builder function.
    /// </summary>
    /// <param name="filterBuilder">Function that builds a filter using the types filter builder.</param>
    ITypesQuery Where(Func<ITypeFilters, IFilter> filterBuilder);

    /// <summary>
    /// Adds a filter to the query.
    /// </summary>
    /// <param name="filter">The filter to add.</param>
    ITypesQuery Where(IFilter filter);

    /// <summary>
    /// Executes the built query.
    /// </summary>
    /// <returns>Delivery types listing response.</returns>
    Task<IDeliveryTypeListingResponse> ExecuteAsync();
}
