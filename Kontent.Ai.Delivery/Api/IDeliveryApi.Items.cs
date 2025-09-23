using Kontent.Ai.Delivery.ContentItems;
using System.Threading;

namespace Kontent.Ai.Delivery.Api;

/// <summary>
/// Refit interface for Kontent.ai Delivery API.
/// </summary>
public partial interface IDeliveryApi
{
    /// <summary>
    /// Gets a single content item by its codename.
    /// </summary>
    /// <typeparam name="TModel">The type of content items in the response.</typeparam>
    /// <param name="codename">The codename of the content item.</param>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the content item.</returns>
    [Get("/items/{codename}")]
    internal Task<IApiResponse<DeliveryItemResponse<TModel>>> GetItemInternalAsync<TModel>(
        string codename,
        [Query] SingleItemParams? queryParameters = null,
        [Header(HttpRequestHeadersExtensions.WaitForLoadingNewContentHeaderName)] bool? waitForLoadingNewContent = null) where TModel : IElementsModel;

    /// <summary>
    /// Gets multiple content items with optional filtering.
    /// </summary>
    /// <typeparam name="TModel">The type of content items in the response.</typeparam>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="filters">Filters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the content items.</returns>
    [Get("/items")]
    internal Task<IApiResponse<DeliveryItemListingResponse<TModel>>> GetItemsInternalAsync<TModel>(
        [Query] ListItemsParams? queryParameters = null,
        [Query] Dictionary<string, string>? filters = null,
        [Header(HttpRequestHeadersExtensions.WaitForLoadingNewContentHeaderName)] bool? waitForLoadingNewContent = null) where TModel : IElementsModel;

    /// <summary>
    /// Gets content items feed for enumeration.
    /// </summary>
    /// <typeparam name="TModel">The type of content items in the response.</typeparam>
    /// <param name="queryParameters">Query parameters for feed enumeration.</param>
    /// <param name="filters">Filters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <param name="continuation">Continuation token for feed enumeration.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the request.</param>
    /// <returns>Raw JSON response containing the items feed.</returns>
    [Get("/items-feed")]
    internal Task<IApiResponse<DeliveryItemsFeedResponse<TModel>>> GetItemsFeedInternalAsync<TModel>(
        [Query] EnumItemsParams? queryParameters = null,
        [Query] Dictionary<string, string>? filters = null,
        [Header("X-Continuation")] string? continuation = null,
        [Header(HttpRequestHeadersExtensions.WaitForLoadingNewContentHeaderName)] bool? waitForLoadingNewContent = null,
        CancellationToken cancellationToken = default) where TModel : IElementsModel;
}
