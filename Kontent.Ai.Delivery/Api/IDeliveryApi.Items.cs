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
    internal Task<IDeliveryItemResponse<T>> GetItemInternalAsync<T>(
        string codename,
        [Query] SingleItemParams? queryParameters = null,
        [Header(HttpRequestHeadersExtensions.WaitForLoadingNewContentHeaderName)] bool? waitForLoadingNewContent = null);

    /// <summary>
    /// Gets multiple content items with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the content items.</returns>
    [Get("/items")]
    internal Task<IDeliveryItemListingResponse<T>> GetItemsInternalAsync<T>(
        [Query] ListItemsParams? queryParameters = null,
        [Header(HttpRequestHeadersExtensions.WaitForLoadingNewContentHeaderName)] bool? waitForLoadingNewContent = null);
}
