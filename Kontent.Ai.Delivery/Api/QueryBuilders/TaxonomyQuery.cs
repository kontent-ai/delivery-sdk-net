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
        return _cacheManager is not null
            ? await ExecuteWithCacheAsync(_cacheManager, cancellationToken).ConfigureAwait(false)
            : await ExecuteWithoutCacheAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IDeliveryResult<ITaxonomyGroup>> ExecuteWithCacheAsync(
        IDeliveryCacheManager cacheManager,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildTaxonomyKey(_codename);
        var (cacheResult, apiResult) = await FetchWithCacheAsync(cacheManager, cacheKey, cancellationToken).ConfigureAwait(false);

        if (cacheResult.IsCacheHit)
            return DeliveryResult.CacheHit<ITaxonomyGroup>(cacheResult.Value!);

        if (apiResult is null)
            throw new InvalidOperationException("API result was not captured during fetch.");

        return apiResult;
    }

    private Task<IDeliveryResult<ITaxonomyGroup>> ExecuteWithoutCacheAsync(CancellationToken cancellationToken)
        => FetchFromApiAsync(cancellationToken);

    private async Task<(CacheFetchResult<TaxonomyGroup> CacheResult, IDeliveryResult<ITaxonomyGroup>? ApiResult)> FetchWithCacheAsync(
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        CancellationToken cancellationToken)
    {
        IDeliveryResult<ITaxonomyGroup>? apiResult = null;

        var cacheResult = await QueryCacheHelper.GetOrFetchAsync(
            cacheManager,
            cacheKey,
            async ct =>
            {
                apiResult = await FetchFromApiAsync(ct).ConfigureAwait(false);
                if (!apiResult.IsSuccess)
                    return (null, Array.Empty<string>());

                return ((TaxonomyGroup)apiResult.Value, Array.Empty<string>());
            },
            logger: null,
            cancellationToken).ConfigureAwait(false);

        return (cacheResult, apiResult);
    }

    private async Task<IDeliveryResult<ITaxonomyGroup>> FetchFromApiAsync(CancellationToken cancellationToken)
    {
        var wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var response = await _api.GetTaxonomyInternalAsync(_codename, wait, cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync().ConfigureAwait(false);
    }
}
