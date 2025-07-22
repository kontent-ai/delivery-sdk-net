using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Refit;

namespace Kontent.Ai.Delivery.Api;

/// <summary>
/// Refit interface for Kontent.ai Delivery API metadata endpoints (content types, taxonomies, languages).
/// </summary>
public interface IDeliveryMetadataApi
{
    /// <summary>
    /// Gets a single content type by its codename.
    /// </summary>
    /// <param name="codename">The codename of the content type.</param>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the content type.</returns>
    [Get("/types/{codename}")]
    Task<JObject> GetTypeAsync(
        string codename,
        [Query(CollectionFormat.Multi)] IDictionary<string, object> queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);

    /// <summary>
    /// Gets multiple content types with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the content types.</returns>
    [Get("/types")]
    Task<JObject> GetTypesAsync(
        [Query(CollectionFormat.Multi)] IDictionary<string, object> queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);

    /// <summary>
    /// Gets a content type element.
    /// </summary>
    /// <param name="contentTypeCodename">The codename of the content type.</param>
    /// <param name="contentElementCodename">The codename of the content element.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the content element.</returns>
    [Get("/types/{contentTypeCodename}/elements/{contentElementCodename}")]
    Task<JObject> GetContentElementAsync(
        string contentTypeCodename,
        string contentElementCodename,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);

    /// <summary>
    /// Gets a single taxonomy group by its codename.
    /// </summary>
    /// <param name="codename">The codename of the taxonomy group.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the taxonomy group.</returns>
    [Get("/taxonomies/{codename}")]
    Task<JObject> GetTaxonomyAsync(
        string codename,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);

    /// <summary>
    /// Gets multiple taxonomy groups with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the taxonomy groups.</returns>
    [Get("/taxonomies")]
    Task<JObject> GetTaxonomiesAsync(
        [Query(CollectionFormat.Multi)] IDictionary<string, object> queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);

    /// <summary>
    /// Gets all active languages assigned to the environment.
    /// </summary>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the languages.</returns>
    [Get("/languages")]
    Task<JObject> GetLanguagesAsync(
        [Query(CollectionFormat.Multi)] IDictionary<string, object> queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);
} 