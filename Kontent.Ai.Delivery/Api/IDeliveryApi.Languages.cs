using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Api.QueryParams.Languages;
using Refit;

namespace Kontent.Ai.Delivery.Api;

public partial interface IDeliveryApi
{
    /// <summary>
    /// Gets all active languages assigned to the environment.
    /// </summary>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the languages.</returns>
    [Get("/languages")]
    internal Task<IDeliveryLanguageListingResponse> GetLanguagesInternalAsync(
        [Query] LanguagesParams? queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);

    // Public forward
    public Task<IDeliveryLanguageListingResponse> GetLanguagesAsync(
        LanguagesParams? queryParameters = null,
        bool? waitForLoadingNewContent = null)
        => GetLanguagesInternalAsync(queryParameters, waitForLoadingNewContent);
}