using Kontent.Ai.Delivery.ContentTypes;

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
    internal Task<IApiResponse<DeliveryTypeResponse>> GetTypeInternalAsync(
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
    internal Task<IApiResponse<DeliveryTypeListingResponse>> GetTypesInternalAsync(
        [Query] ListTypesParams? queryParameters = null,
        [Header(HttpRequestHeadersExtensions.WaitForLoadingNewContentHeaderName)] bool? waitForLoadingNewContent = null);

    /// <summary>
    /// Gets a content type element.
    /// </summary>
    /// <param name="contentTypeCodename">The codename of the content type.</param>
    /// <param name="contentElementCodename">The codename of the content element.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the content element.</returns>
    [Get("/types/{contentTypeCodename}/elements/{contentElementCodename}")]
    internal Task<IApiResponse<DeliveryElementResponse>> GetContentElementInternalAsync(
        string contentTypeCodename,
        string contentElementCodename,
        [Header(HttpRequestHeadersExtensions.WaitForLoadingNewContentHeaderName)] bool? waitForLoadingNewContent = null);
}