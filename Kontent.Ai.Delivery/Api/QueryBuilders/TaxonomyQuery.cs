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
        // 1. CACHE CHECK
        string? cacheKey = null;
        if (_cacheManager != null)
        {
            cacheKey = CacheKeyBuilder.BuildTaxonomyKey(_codename);
            var cached = await QueryCacheHelper.TryGetCachedAsync<TaxonomyGroup>(
                _cacheManager, cacheKey, logger: null, cancellationToken).ConfigureAwait(false);
            if (cached != null)
                return DeliveryResult.CacheHit<ITaxonomyGroup>(cached);
        }

        // 2. API CALL
        var deliveryResult = await FetchFromApiAsync().ConfigureAwait(false);

        // 3. CACHE RESULT
        // Metadata queries use empty dependencies (rely on TTL for invalidation)
        if (_cacheManager != null && deliveryResult.IsSuccess && cacheKey != null)
        {
            await QueryCacheHelper.TrySetCachedAsync(
                _cacheManager, cacheKey, (TaxonomyGroup)deliveryResult.Value,
                dependencies: [], logger: null, cancellationToken).ConfigureAwait(false);
        }

        return deliveryResult;
    }

    private async Task<IDeliveryResult<ITaxonomyGroup>> FetchFromApiAsync()
    {
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var response = await _api.GetTaxonomyInternalAsync(_codename, wait).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync().ConfigureAwait(false);
    }
}