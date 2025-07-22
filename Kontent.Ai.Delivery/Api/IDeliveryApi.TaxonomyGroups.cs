using System.Threading.Tasks;
using Kontent.Ai.Delivery.Api.QueryParams.TaxonomyGroups;
using Kontent.Ai.Delivery.Abstractions;
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
    Task<IDeliveryTaxonomyResponse> GetTaxonomyAsync(
        string codename,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);

    /// <summary>
    /// Gets multiple taxonomy groups with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the taxonomy groups.</returns>
    [Get("/taxonomies")]
    Task<IDeliveryTaxonomyListingResponse> GetTaxonomiesAsync(
        [Query] IListTaxonomyGroupsParams? queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);
}