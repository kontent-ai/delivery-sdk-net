using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.TaxonomyGroups;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITaxonomyQuery"/>
internal sealed class TaxonomyQuery(
    IDeliveryApi api,
    string codename,
    Func<bool?> getDefaultWaitForNewContent,
    IDeliveryCacheManager? cacheManager) : ITaxonomyQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;

    public ITaxonomyQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async Task<IDeliveryResult<ITaxonomyGroup>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_cacheManager != null)
        {
            var cacheKey = CacheKeyBuilder.BuildTaxonomyKey(_codename);
            IDeliveryResult<ITaxonomyGroup>? apiResult = null;

            // Use stampede-protected cache fetch
            var cacheResult = await QueryCacheHelper.GetOrFetchAsync(
                _cacheManager,
                cacheKey,
                async ct =>
                {
                    apiResult = await FetchFromApiAsync(ct).ConfigureAwait(false);
                    if (!apiResult.IsSuccess)
                        return (null, Array.Empty<string>());
                    // Metadata queries use empty dependencies (rely on TTL for invalidation)
                    return ((TaxonomyGroup)apiResult.Value, Array.Empty<string>());
                },
                logger: null,
                cancellationToken).ConfigureAwait(false);

            if (cacheResult.IsCacheHit)
                return DeliveryResult.CacheHit<ITaxonomyGroup>(cacheResult.Value!);

            // Not a cache hit - return the API result (success or failure)
            return apiResult!;
        }

        // No caching - direct fetch
        return await FetchFromApiAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IDeliveryResult<ITaxonomyGroup>> FetchFromApiAsync(CancellationToken cancellationToken)
    {
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var response = await _api.GetTaxonomyInternalAsync(_codename, wait, cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync().ConfigureAwait(false);
    }
}
