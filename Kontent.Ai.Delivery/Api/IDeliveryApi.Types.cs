using System.Threading.Tasks;
using Kontent.Ai.Delivery.Api.QueryParams.Types;
using Kontent.Ai.Delivery.Abstractions;
using Refit;
using Kontent.Ai.Delivery.Extensions;

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
    internal Task<IDeliveryTypeResponse> GetTypeInternalAsync(
        string codename,
        [Query] SingleTypeParams? queryParameters = null,
        [Header(HttpRequestHeadersExtensions.WaitForLoadingNewContentHeaderName)] bool? waitForLoadingNewContent = null);

    /// <summary>
    /// Gets multiple content types with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the content types.</returns>
    [Get("/types")]
    internal Task<IDeliveryTypeListingResponse> GetTypesInternalAsync(
        [Query] ListTypesParams? queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);

    /// <summary>
    /// Gets a content type element.
    /// </summary>
    /// <param name="contentTypeCodename">The codename of the content type.</param>
    /// <param name="contentElementCodename">The codename of the content element.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the content element.</returns>
    [Get("/types/{contentTypeCodename}/elements/{contentElementCodename}")]
    internal Task<IDeliveryElementResponse> GetContentElementInternalAsync(
        string contentTypeCodename,
        string contentElementCodename,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);

    // Default, public forwards for convenience (non-fluent for now)
    /// <summary>
    /// Gets a single content type by its codename.
    /// </summary>
    /// <param name="codename">The codename of the content type.</param>
    /// <param name="queryParameters">Optional query parameters.</param>
    /// <param name="waitForLoadingNewContent">Optional new content wait header.</param>
    public Task<IDeliveryTypeResponse> GetTypeAsync(
        string codename,
        SingleTypeParams? queryParameters = null,
        bool? waitForLoadingNewContent = null)
        => GetTypeInternalAsync(codename, queryParameters, waitForLoadingNewContent);

    /// <summary>
    /// Gets multiple content types with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Optional query parameters.</param>
    /// <param name="waitForLoadingNewContent">Optional new content wait header.</param>
    public Task<IDeliveryTypeListingResponse> GetTypesAsync(
        ListTypesParams? queryParameters = null,
        bool? waitForLoadingNewContent = null)
        => GetTypesInternalAsync(queryParameters, waitForLoadingNewContent);

    /// <summary>
    /// Gets a content type element by codename.
    /// </summary>
    /// <param name="contentTypeCodename">Content type codename.</param>
    /// <param name="contentElementCodename">Element codename.</param>
    /// <param name="waitForLoadingNewContent">Optional new content wait header.</param>
    public Task<IDeliveryElementResponse> GetContentElementAsync(
        string contentTypeCodename,
        string contentElementCodename,
        bool? waitForLoadingNewContent = null)
        => GetContentElementInternalAsync(contentTypeCodename, contentElementCodename, waitForLoadingNewContent);
}