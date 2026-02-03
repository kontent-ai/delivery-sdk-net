namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Fluent builder for listing content items.
/// </summary>
/// <typeparam name="TModel">Strongly typed elements model of the content items.</typeparam>
public interface IItemsQuery<TModel>
{
    /// <summary>
    /// Sets the language codename for the request.
    /// </summary>
    /// <param name="languageCodename">Language codename.</param>
    /// <param name="languageFallbackMode">Language fallback mode.</param>
    /// <returns>The query builder for method chaining.</returns>
    IItemsQuery<TModel> WithLanguage(string languageCodename, LanguageFallbackMode languageFallbackMode = LanguageFallbackMode.Enabled);

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
    /// <param name="orderingMode">Ordering mode (ascending/descending).</param>
    /// <returns>The query builder for method chaining.</returns>
    IItemsQuery<TModel> OrderBy(string elementOrAttributePath, OrderingMode orderingMode = OrderingMode.Ascending);

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
    /// Adds filtering conditions to the query.
    /// </summary>
    /// <remarks>
    /// The returned query uses AND semantics between conditions (multiple query parameters).
    /// </remarks>
    /// <param name="build">Builder function that appends one or more filtering conditions.</param>
    /// <returns>The query builder for method chaining.</returns>
    IItemsQuery<TModel> Where(Func<IItemsFilterBuilder, IItemsFilterBuilder> build);

    /// <summary>
    /// Executes the built query and returns the response with pagination metadata.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A delivery result containing the response with items and pagination info.</returns>
    Task<IDeliveryResult<IDeliveryItemListingResponse<TModel>>> ExecuteAsync(CancellationToken cancellationToken = default);
}
