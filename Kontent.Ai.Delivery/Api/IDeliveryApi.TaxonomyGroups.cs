using Kontent.Ai.Delivery.TaxonomyGroups;

namespace Kontent.Ai.Delivery.Api;

/// <inheritdoc cref="IDeliveryApi"/>
public partial interface IDeliveryApi
{
    /// <summary>
    /// Gets a single taxonomy group by its codename.
    /// </summary>
    /// <param name="codename">The codename of the taxonomy group.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the taxonomy group.</returns>
    [Get("/taxonomies/{codename}")]
    internal Task<IApiResponse<TaxonomyGroup>> GetTaxonomyInternalAsync(
        string codename,
        [Header(HttpRequestHeadersExtensions.WaitForLoadingNewContentHeaderName)] bool? waitForLoadingNewContent = null);

    /// <summary>
    /// Gets multiple taxonomy groups with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the taxonomy groups.</returns>
    [Get("/taxonomies")]
    internal Task<IApiResponse<DeliveryTaxonomyListingResponse>> GetTaxonomiesInternalAsync(
        [Query] ListTaxonomyGroupsParams? queryParameters = null,
        [Header(HttpRequestHeadersExtensions.WaitForLoadingNewContentHeaderName)] bool? waitForLoadingNewContent = null);
}