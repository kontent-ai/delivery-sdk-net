namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Fluent builder for retrieving multiple content items with dynamic content mapping.
/// </summary>
public interface IDynamicItemsQuery
{
    /// <summary>
    /// Sets the language codename for the request.
    /// </summary>
    /// <param name="languageCodename">Language codename.</param>
    /// <param name="languageFallbackMode">Language fallback mode.</param>
    /// <returns>The query builder for method chaining.</returns>
    IDynamicItemsQuery WithLanguage(string languageCodename, LanguageFallbackMode languageFallbackMode = LanguageFallbackMode.Enabled);

    /// <summary>
    /// Includes only specified element codenames in the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to include.</param>
    /// <returns>The query builder for method chaining.</returns>
    IDynamicItemsQuery WithElements(params string[] elementCodenames);

    /// <summary>
    /// Excludes specified element codenames from the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to exclude.</param>
    /// <returns>The query builder for method chaining.</returns>
    IDynamicItemsQuery WithoutElements(params string[] elementCodenames);

    /// <summary>
    /// Sets the linked items depth.
    /// </summary>
    /// <param name="depth">Depth value.</param>
    /// <returns>The query builder for method chaining.</returns>
    IDynamicItemsQuery Depth(int depth);

    /// <summary>
    /// Skips the specified number of content items.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <returns>The query builder for method chaining.</returns>
    IDynamicItemsQuery Skip(int skip);

    /// <summary>
    /// Limits the number of content items.
    /// </summary>
    /// <param name="limit">Maximum number of items to retrieve.</param>
    /// <returns>The query builder for method chaining.</returns>
    IDynamicItemsQuery Limit(int limit);

    /// <summary>
    /// Orders the results by the specified element or system attribute.
    /// </summary>
    /// <param name="elementOrAttributePath">Element or attribute path for ordering.</param>
    /// <param name="ascending">Whether to order in ascending order.</param>
    /// <returns>The query builder for method chaining.</returns>
    IDynamicItemsQuery OrderBy(string elementOrAttributePath, bool ascending = true);

    /// <summary>
    /// Includes the total count in the response for pagination purposes.
    /// </summary>
    /// <returns>The query builder for method chaining.</returns>
    IDynamicItemsQuery WithTotalCount();

    /// <summary>
    /// Overrides the global option for waiting on the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    /// <returns>The query builder for method chaining.</returns>
    IDynamicItemsQuery WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Adds filtering conditions to the query.
    /// </summary>
    /// <remarks>
    /// The returned query uses AND semantics between conditions (multiple query parameters).
    /// </remarks>
    /// <param name="build">Builder function that appends one or more filtering conditions.</param>
    /// <returns>The query builder for method chaining.</returns>
    IDynamicItemsQuery Where(Func<IItemsFilterBuilder, IItemsFilterBuilder> build);

    /// <summary>
    /// Executes the built query and returns a functional result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A delivery result containing the content items or errors.</returns>
    Task<IDeliveryResult<IReadOnlyList<IContentItem<IDynamicElements>>>> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the built query to retrieve all content items across all pages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A delivery result containing all content items or errors.</returns>
    Task<IDeliveryResult<IReadOnlyList<IContentItem<IDynamicElements>>>> ExecuteAllAsync(CancellationToken cancellationToken = default);
}