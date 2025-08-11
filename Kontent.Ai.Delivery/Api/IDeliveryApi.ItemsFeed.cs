using System.Threading.Tasks;
using Refit;

namespace Kontent.Ai.Delivery.Api;

/// <inheritdoc cref="IDeliveryApi"/>
public partial interface IDeliveryApi
{
    /// <summary>
    /// Gets content items feed for continuous enumeration.
    /// </summary>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <param name="continuationToken">Continuation token for feed operations.</param>
    /// <returns>Response containing a batch of content items and continuation token.</returns>
    [Get("/items-feed")]
    internal Task<IDeliveryItemsFeedResponse<T>> GetItemsFeedInternalAsync<T>(
        [Query] Api.QueryParams.Items.EnumItemsParams? queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null,
        [Header("X-Continuation")] string? continuationToken = null);

    /// <summary>
    /// Gets content items that use the specified content item.
    /// </summary>
    /// <param name="codename">The codename of the content item.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <param name="continuationToken">Continuation token for operations.</param>
    /// <returns>Response containing a batch of parent items and continuation token.</returns>
    [Get("/items/{codename}/used-in")]
    internal Task<IDeliveryItemsFeedResponse<IUsedInItem>> GetItemUsedInInternalAsync(
        string codename,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null,
        [Header("X-Continuation")] string? continuationToken = null);

    /// <summary>
    /// Gets content items that use the specified asset.
    /// </summary>
    /// <param name="codename">The codename of the asset.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <param name="continuationToken">Continuation token for operations.</param>
    /// <returns>Response containing a batch of parent items and continuation token.</returns>
    [Get("/assets/{codename}/used-in")]
    internal Task<IDeliveryItemsFeedResponse<IUsedInItem>> GetAssetUsedInInternalAsync(
        string codename,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null,
        [Header("X-Continuation")] string? continuationToken = null);
}