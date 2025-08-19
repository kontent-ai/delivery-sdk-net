using Kontent.Ai.Delivery.Api.ResponseModels;

namespace Kontent.Ai.Delivery.Api;

/// <inheritdoc cref="IDeliveryApi"/>
public partial interface IDeliveryApi
{
    /// <summary>
    /// Gets content items that use the specified content item.
    /// </summary>
    /// <param name="codename">The codename of the content item.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <param name="continuationToken">Continuation token for operations.</param>
    /// <returns>Response containing a batch of parent items and continuation token.</returns>
    [Get("/items/{codename}/used-in")]
    internal Task<IApiResponse<RawUsedInResponse>> GetItemUsedInInternalAsync(
        string codename,
        [Header(HttpRequestHeadersExtensions.WaitForLoadingNewContentHeaderName)] bool? waitForLoadingNewContent = null,
        [Header("X-Continuation")] string? continuationToken = null);

    /// <summary>
    /// Gets content items that use the specified asset.
    /// </summary>
    /// <param name="codename">The codename of the asset.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <param name="continuationToken">Continuation token for operations.</param>
    /// <returns>Response containing a batch of parent items and continuation token.</returns>
    [Get("/assets/{codename}/used-in")]
    internal Task<IApiResponse<RawUsedInResponse>> GetAssetUsedInInternalAsync(
        string codename,
        [Header(HttpRequestHeadersExtensions.WaitForLoadingNewContentHeaderName)] bool? waitForLoadingNewContent = null,
        [Header("X-Continuation")] string? continuationToken = null);
}