using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Api.QueryParams.Items;
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
    /// <returns>Raw JSON response containing the content items feed.</returns>
    [Get("/items-feed")]
    Task<IDeliveryItemsFeed<T>> GetItemsFeedAsync<T>(
        [Query] IEnumItemsParams? queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null,
        [Header("X-Continuation")] string? continuationToken = null);

    /// <summary>
    /// Gets content items that use the specified content item.
    /// </summary>
    /// <param name="codename">The codename of the content item.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <param name="continuationToken">Continuation token for operations.</param>
    /// <returns>Raw JSON response containing the content items that use the specified item.</returns>
    [Get("/items/{codename}/used-in")]
    Task<IDeliveryItemsFeed<IUsedInItem>> GetItemUsedInAsync(
        string codename,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null,
        [Header("X-Continuation")] string? continuationToken = null);

    /// <summary>
    /// Gets content items that use the specified asset.
    /// </summary>
    /// <param name="codename">The codename of the asset.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <param name="continuationToken">Continuation token for operations.</param>
    /// <returns>Raw JSON response containing the content items that use the specified asset.</returns>
    [Get("/assets/{codename}/used-in")]
    Task<IDeliveryItemsFeed<IUsedInItem>> GetAssetUsedInAsync(
        string codename,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null,
        [Header("X-Continuation")] string? continuationToken = null);
}