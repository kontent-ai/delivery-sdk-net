using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Refit;

namespace Kontent.Ai.Delivery.Api;

/// <summary>
/// Refit interface for Kontent.ai Delivery API sync endpoints.
/// </summary>
public interface IDeliverySyncApi
{
    /// <summary>
    /// Initializes synchronization of changes in content items.
    /// </summary>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the sync initialization data.</returns>
    [Post("/sync/init")]
    Task<JObject> PostSyncInitAsync(
        [Query(CollectionFormat.Multi)] IDictionary<string, object> queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);

    /// <summary>
    /// Retrieves a list of delta updates to recently changed content items.
    /// </summary>
    /// <param name="continuationToken">Continuation token from sync initialization or previous sync call.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the sync delta updates.</returns>
    [Get("/sync")]
    Task<JObject> GetSyncAsync(
        [Header("X-Continuation")] string continuationToken,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);


} 