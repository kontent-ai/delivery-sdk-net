using System.Collections.Generic;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Api.QueryParams.Types;
using Newtonsoft.Json.Linq;
using Refit;

namespace Kontent.Ai.Delivery.Api;

/// <inheritdoc cref="IDeliveryApi"/>
public partial interface IDeliveryApi
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
        [Query] ISingleTypeParams? queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);

    /// <summary>
    /// Gets multiple content types with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the content types.</returns>
    [Get("/types")]
    Task<JObject> GetTypesAsync(
        [Query] IListTypesParams? queryParameters = null,
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
}