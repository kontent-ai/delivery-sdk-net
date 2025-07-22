using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Refit;

namespace Kontent.Ai.Delivery.Api;

/// <summary>
/// Refit interface for Kontent.ai Delivery API sync v2 endpoints.
/// </summary>
public interface IDeliverySyncV2Api
{
    /// <summary>
    /// Initializes synchronization of changes in content items, content types, taxonomies and languages (v2).
    /// </summary>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the sync v2 initialization data.</returns>
    [Post("/sync/init")]
    Task<JObject> PostSyncV2InitAsync(
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);

    /// <summary>
    /// Retrieves a list of delta updates to recently changed content items, content types, taxonomies or languages (v2).
    /// </summary>
    /// <param name="continuationToken">Continuation token from sync initialization or previous sync call.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the sync v2 delta updates.</returns>
    [Get("/sync")]
    Task<JObject> GetSyncV2Async(
        [Header("X-Continuation")] string continuationToken,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);
} 