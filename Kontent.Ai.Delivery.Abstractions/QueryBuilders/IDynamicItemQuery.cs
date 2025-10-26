namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Fluent builder for retrieving a single content item by codename with dynamic content mapping.
/// </summary>
public interface IDynamicItemQuery
{
    /// <summary>
    /// Sets the language codename for the request.
    /// </summary>
    /// <param name="languageCodename">Language codename.</param>
    /// <returns>The query builder for method chaining.</returns>
    IDynamicItemQuery WithLanguage(string languageCodename);

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
    /// <returns>A delivery result containing the content item or errors.</returns>
    Task<IDeliveryResult<IContentItem<IElementsModel>>> ExecuteAsync(CancellationToken cancellationToken = default);
}