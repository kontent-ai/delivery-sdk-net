using System.Threading;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions.SharedModels;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders;

/// <summary>
/// Fluent builder for retrieving a single content item by codename.
/// </summary>
/// <typeparam name="T">The type of the content item.</typeparam>
public interface ISingleItemQuery<T>
{
    /// <summary>
    /// Sets the language codename for the request.
    /// </summary>
    /// <param name="languageCodename">Language codename.</param>
    /// <returns>The query builder for method chaining.</returns>
    ISingleItemQuery<T> WithLanguage(string languageCodename);

    /// <summary>
    /// Includes only specified element codenames in the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to include.</param>
    /// <returns>The query builder for method chaining.</returns>
    ISingleItemQuery<T> WithElements(params string[] elementCodenames);

    /// <summary>
    /// Excludes specified element codenames from the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to exclude.</param>
    /// <returns>The query builder for method chaining.</returns>
    ISingleItemQuery<T> WithoutElements(params string[] elementCodenames);

    /// <summary>
    /// Sets the linked items depth.
    /// </summary>
    /// <param name="depth">Depth value.</param>
    /// <returns>The query builder for method chaining.</returns>
    ISingleItemQuery<T> Depth(int depth);

    /// <summary>
    /// Overrides the global option for waiting on the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    /// <returns>The query builder for method chaining.</returns>
    ISingleItemQuery<T> WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Executes the built query and returns a functional result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A delivery result containing the content item or errors.</returns>
    Task<IDeliveryResult<T>> ExecuteAsync(CancellationToken cancellationToken = default);
}
