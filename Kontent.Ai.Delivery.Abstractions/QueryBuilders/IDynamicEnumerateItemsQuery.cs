namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Fluent builder for enumerating content items with runtime type resolution support.
/// </summary>
/// <remarks>
/// When a custom <see cref="ITypeProvider"/> is registered, items will be automatically
/// resolved to their strongly-typed models at runtime. Use pattern matching to access typed items:
/// <code>
/// await foreach (var item in client.GetItemsFeed().EnumerateItemsAsync())
/// {
///     if (item is IContentItem&lt;Article&gt; article)
///         Console.WriteLine(article.Elements.Title);
/// }
/// </code>
/// </remarks>
public interface IDynamicEnumerateItemsQuery
{
    /// <summary>
    /// Sets the language codename for the request.
    /// </summary>
    /// <param name="languageCodename">Language codename.</param>
    /// <param name="languageFallbackMode">Language fallback mode.</param>
    IDynamicEnumerateItemsQuery WithLanguage(string languageCodename, LanguageFallbackMode languageFallbackMode = LanguageFallbackMode.Enabled);

    /// <summary>
    /// Includes only specified element codenames in the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to include.</param>
    IDynamicEnumerateItemsQuery WithElements(params string[] elementCodenames);

    /// <summary>
    /// Orders the items by the given path in ascending or descending order.
    /// </summary>
    /// <param name="elementOrAttributePath">Element or attribute path.</param>
    /// <param name="orderingMode">Ordering mode (ascending/descending).</param>
    IDynamicEnumerateItemsQuery OrderBy(string elementOrAttributePath, OrderingMode orderingMode = OrderingMode.Ascending);

    /// <summary>
    /// Overrides the global option for waiting on the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    IDynamicEnumerateItemsQuery WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Adds filtering conditions to the query.
    /// </summary>
    /// <remarks>
    /// The returned query uses AND semantics between conditions (multiple query parameters).
    /// </remarks>
    /// <param name="build">Builder function that appends one or more filtering conditions.</param>
    /// <returns>The query builder for method chaining.</returns>
    IDynamicEnumerateItemsQuery Where(Func<IItemsFilterBuilder, IItemsFilterBuilder> build);

    /// <summary>
    /// Executes the query and returns the first page of items.
    /// Use <see cref="IDeliveryItemsFeedResponse.FetchNextPageAsync"/> to retrieve subsequent pages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The first page of items with ability to fetch subsequent pages.
    /// Items will be runtime-typed if a custom <see cref="ITypeProvider"/> is registered.
    /// </returns>
    Task<IDeliveryResult<IDeliveryItemsFeedResponse>> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates content items using the Delivery API items-feed endpoint.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop enumeration and cancel in-flight requests.</param>
    /// <returns>
    /// Async sequence of content items. Each item will be runtime-typed if a custom
    /// <see cref="ITypeProvider"/> is registered and provides a mapping.
    /// </returns>
    IAsyncEnumerable<IContentItem> EnumerateItemsAsync(CancellationToken cancellationToken = default);
}
