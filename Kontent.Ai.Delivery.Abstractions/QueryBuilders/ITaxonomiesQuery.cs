using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Fluent builder for listing taxonomy groups.
/// </summary>
public interface ITaxonomiesQuery
{
    /// <summary>
    /// Sets the number of taxonomy groups to skip.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    ITaxonomiesQuery Skip(int skip);

    /// <summary>
    /// Sets the maximum number of taxonomy groups to return.
    /// </summary>
    /// <param name="limit">Maximum number of items.</param>
    ITaxonomiesQuery Limit(int limit);

    /// <summary>
    /// Adds a filter to the query using a filter builder function.
    /// </summary>
    /// <param name="filterBuilder">Function that builds a filter using the taxonomy filter builder.</param>
    ITaxonomiesQuery Where(Func<ITaxonomyFilters, IFilter> filterBuilder);

    /// <summary>
    /// Adds a filter to the query.
    /// </summary>
    /// <param name="filter">The filter to add.</param>
    ITaxonomiesQuery Where(IFilter filter);

    /// <summary>
    /// Overrides the global option for waiting on the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    ITaxonomiesQuery WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Executes the built query and returns a functional result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A delivery result containing the taxonomies or errors.</returns>
    Task<IDeliveryResult<IReadOnlyList<ITaxonomyGroup>>> ExecuteAsync(CancellationToken cancellationToken = default);
}
