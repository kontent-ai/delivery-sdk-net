using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Refit;

namespace Kontent.Ai.Delivery.Api;

/// <summary>
/// Refit interface for Kontent.ai Delivery API content items endpoints.
/// </summary>
public interface IDeliveryContentApi
{
    /// <summary>
    /// Gets a single content item by its codename.
    /// </summary>
    /// <param name="codename">The codename of the content item.</param>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <param name="continuationToken">Continuation token for sync operations.</param>
    /// <returns>Raw JSON response containing the content item.</returns>
    [Get("/items/{codename}")]
    Task<JObject> GetItemAsync(
        string codename,
        [Query(CollectionFormat.Multi)] IDictionary<string, object> queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null,
        [Header("X-Continuation")] string continuationToken = null);

    /// <summary>
    /// Gets multiple content items with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <param name="continuationToken">Continuation token for sync operations.</param>
    /// <returns>Raw JSON response containing the content items.</returns>
    [Get("/items")]
    Task<JObject> GetItemsAsync(
        [Query(CollectionFormat.Multi)] IDictionary<string, object> queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null,
        [Header("X-Continuation")] string continuationToken = null);

    /// <summary>
    /// Gets content items feed for continuous enumeration.
    /// </summary>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <param name="continuationToken">Continuation token for feed operations.</param>
    /// <returns>Raw JSON response containing the content items feed.</returns>
    [Get("/items-feed")]
    Task<JObject> GetItemsFeedAsync(
        [Query(CollectionFormat.Multi)] IDictionary<string, object> queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null,
        [Header("X-Continuation")] string continuationToken = null);

    /// <summary>
    /// Gets content items that use the specified content item.
    /// </summary>
    /// <param name="codename">The codename of the content item.</param>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <param name="continuationToken">Continuation token for operations.</param>
    /// <returns>Raw JSON response containing the content items that use the specified item.</returns>
    [Get("/items/{codename}/used-in")]
    Task<JObject> GetItemUsedInAsync(
        string codename,
        [Query(CollectionFormat.Multi)] IDictionary<string, object> queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null,
        [Header("X-Continuation")] string continuationToken = null);

    /// <summary>
    /// Gets content items that use the specified asset.
    /// </summary>
    /// <param name="codename">The codename of the asset.</param>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <param name="continuationToken">Continuation token for operations.</param>
    /// <returns>Raw JSON response containing the content items that use the specified asset.</returns>
    [Get("/assets/{codename}/used-in")]
    Task<JObject> GetAssetUsedInAsync(
        string codename,
        [Query(CollectionFormat.Multi)] IDictionary<string, object> queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null,
        [Header("X-Continuation")] string continuationToken = null);
} 