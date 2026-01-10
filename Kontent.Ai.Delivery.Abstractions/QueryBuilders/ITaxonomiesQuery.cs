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
    /// Adds filtering conditions to the query.
    /// </summary>
    /// <remarks>
    /// The returned query uses AND semantics between conditions (multiple query parameters).
    /// </remarks>
    /// <param name="build">Builder function that appends one or more filtering conditions.</param>
    ITaxonomiesQuery Where(Func<ITaxonomiesFilterBuilder, ITaxonomiesFilterBuilder> build);

    /// <summary>
    /// Overrides the global option for waiting on the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    ITaxonomiesQuery WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Executes the built query and returns a functional result.
    /// Use <see cref="IDeliveryTaxonomyListingResponse.FetchNextPageAsync"/> to retrieve subsequent pages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A delivery result containing the taxonomy groups with pagination support.</returns>
    Task<IDeliveryResult<IDeliveryTaxonomyListingResponse>> ExecuteAsync(CancellationToken cancellationToken = default);
}