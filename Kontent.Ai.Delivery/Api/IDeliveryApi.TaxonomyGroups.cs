using System.Threading.Tasks;
using Refit;

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
    internal Task<IDeliveryTaxonomyResponse> GetTaxonomyInternalAsync(
        string codename,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);

    /// <summary>
    /// Gets multiple taxonomy groups with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the taxonomy groups.</returns>
    [Get("/taxonomies")]
    internal Task<IDeliveryTaxonomyListingResponse> GetTaxonomiesInternalAsync(
        [Query] Api.QueryParams.TaxonomyGroups.ListTaxonomyGroupsParams? queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);
/// <inheritdoc/>

    // Public forwards
    public Task<IDeliveryTaxonomyResponse> GetTaxonomyAsync(
        string codename,
        bool? waitForLoadingNewContent = null)
        => GetTaxonomyInternalAsync(codename, waitForLoadingNewContent);
/// <inheritdoc/>

    public Task<IDeliveryTaxonomyListingResponse> GetTaxonomiesAsync(
        Api.QueryParams.TaxonomyGroups.ListTaxonomyGroupsParams? queryParameters = null,
        bool? waitForLoadingNewContent = null)
        => GetTaxonomiesInternalAsync(queryParameters, waitForLoadingNewContent);
}