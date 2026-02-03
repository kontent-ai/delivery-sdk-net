namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Fluent builder for retrieving a single content item by codename with dynamic content mapping.
/// </summary>
/// <remarks>
/// When a custom <see cref="ITypeProvider"/> is registered, the item will be automatically
/// resolved to its strongly-typed model at runtime. Use pattern matching to access the typed item:
/// <code>
/// var result = await client.GetItem("codename").ExecuteAsync();
/// if (result.Value is IContentItem&lt;Article&gt; article)
/// {
///     var title = article.Elements.Title;
/// }
/// </code>
/// </remarks>
public interface IDynamicItemQuery
{
    /// <summary>
    /// Sets the language codename for the request.
    /// </summary>
    /// <param name="languageCodename">Language codename.</param>
    /// <param name="languageFallbackMode">Language fallback mode.</param>
    /// <returns>The query builder for method chaining.</returns>
    IDynamicItemQuery WithLanguage(string languageCodename, LanguageFallbackMode languageFallbackMode = LanguageFallbackMode.Enabled);

    /// <summary>
    /// Includes only specified element codenames in the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to include.</param>
    /// <returns>The query builder for method chaining.</returns>
    IDynamicItemQuery WithElements(params string[] elementCodenames);

    /// <summary>
    /// Excludes specified element codenames from the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to exclude.</param>
    /// <returns>The query builder for method chaining.</returns>
    IDynamicItemQuery WithoutElements(params string[] elementCodenames);

    /// <summary>
    /// Sets the linked items depth.
    /// </summary>
    /// <param name="depth">Depth value.</param>
    /// <returns>The query builder for method chaining.</returns>
    IDynamicItemQuery Depth(int depth);

    /// <summary>
    /// Overrides the global option for waiting on the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    /// <returns>The query builder for method chaining.</returns>
    IDynamicItemQuery WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Executes the built query and returns a functional result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A delivery result containing the content item. The item will be runtime-typed
    /// if a custom <see cref="ITypeProvider"/> is registered and provides a mapping,
    /// otherwise it will be <see cref="IContentItem{IDynamicElements}"/>.
    /// </returns>
    Task<IDeliveryResult<IContentItem>> ExecuteAsync(CancellationToken cancellationToken = default);
}
