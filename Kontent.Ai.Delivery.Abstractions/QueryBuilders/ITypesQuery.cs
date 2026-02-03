namespace Kontent.Ai.Delivery.Abstractions;

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
    /// Adds filtering conditions to the query.
    /// </summary>
    /// <remarks>
    /// The returned query uses AND semantics between conditions (multiple query parameters).
    /// </remarks>
    /// <param name="build">Builder function that appends one or more filtering conditions.</param>
    ITypesQuery Where(Func<ITypesFilterBuilder, ITypesFilterBuilder> build);

    /// <summary>
    /// Overrides the global option for waiting on the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    ITypesQuery WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Executes the built query and returns a functional result.
    /// Use <see cref="IDeliveryTypeListingResponse.FetchNextPageAsync"/> to retrieve subsequent pages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A delivery result containing the content types with pagination support.</returns>
    Task<IDeliveryResult<IDeliveryTypeListingResponse>> ExecuteAsync(CancellationToken cancellationToken = default);
}
