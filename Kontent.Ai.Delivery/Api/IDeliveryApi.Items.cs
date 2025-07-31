using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Refit;

namespace Kontent.Ai.Delivery.Api;

/// <summary>
/// Refit interface for Kontent.ai Delivery API.
/// </summary>
public partial interface IDeliveryApi
{
    /// <summary>
    /// Gets a single content item by its codename.
    /// </summary>
    /// <param name="codename">The codename of the content item.</param>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the content item.</returns>
    [Get("/items/{codename}")]
    Task<IDeliveryItemResponse<T>> GetItemAsync<T>(
        string codename,
        [Query(CollectionFormat.Multi)] ISingleItemQueryParams? queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);

    /// <summary>
    /// Gets multiple content items with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the content items.</returns>
    [Get("/items")]
    Task<IDeliveryItemListingResponse<T>> GetItemsAsync<T>(
        [Query] IListingItemQueryParams? queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);
}