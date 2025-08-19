using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions.SharedModels;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders;

/// <summary>
/// Fluent builder for enumerating content items.
/// </summary>
public interface IEnumerateItemsQuery<T>
{
    /// <summary>
    /// Sets the language codename for the request.
    /// </summary>
    /// <param name="languageCodename">Language codename.</param>
    IEnumerateItemsQuery<T> WithLanguage(string languageCodename);
    /// <summary>
    /// Includes only specified element codenames in the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to include.</param>
    IEnumerateItemsQuery<T> WithElements(params string[] elementCodenames);
    /// <summary>
    /// Orders the items by the given path in ascending or descending order.
    /// </summary>
    /// <param name="elementOrAttributePath">Element or attribute path.</param>
    /// <param name="ascending">True for ascending; false for descending.</param>
    IEnumerateItemsQuery<T> OrderBy(string elementOrAttributePath, bool ascending = true);

    /// <summary>
    /// Overrides the global option for waiting on the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    IEnumerateItemsQuery<T> WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Executes the built query and returns a functional result.
    /// </summary>
    /// <returns>A delivery result containing the items feed or errors.</returns>
    Task<IDeliveryResult<IDeliveryItemsFeedResponse<T>>> ExecuteAsync();
}
