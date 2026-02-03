using Kontent.Ai.Delivery.Languages;

namespace Kontent.Ai.Delivery.Api;

internal partial interface IDeliveryApi
{
    /// <summary>
    /// Gets all active languages assigned to the environment.
    /// </summary>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the request.</param>
    /// <returns>Raw JSON response containing the languages.</returns>
    [Get("/languages")]
    internal Task<IApiResponse<DeliveryLanguageListingResponse>> GetLanguagesInternalAsync(
        [Query] LanguagesParams? queryParameters = null,
        [Header(HttpRequestHeadersExtensions.WaitForLoadingNewContentHeaderName)] bool? waitForLoadingNewContent = null,
        CancellationToken cancellationToken = default);
}
