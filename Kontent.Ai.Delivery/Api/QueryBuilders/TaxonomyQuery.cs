using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.TaxonomyGroups;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITaxonomyQuery"/>
internal sealed class TaxonomyQuery(
    IDeliveryApi api,
    string codename,
    IDeliveryCacheManager? cacheManager) : ITaxonomyQuery, ICacheExpirationConfigurable
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private bool _waitForLoadingNewContent;
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;
    private TimeSpan? _cacheExpiration;
    TimeSpan? ICacheExpirationConfigurable.CacheExpiration { get => _cacheExpiration; set => _cacheExpiration = value; }

    public ITaxonomyQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContent = enabled;
        return this;
    }

    public async Task<IDeliveryResult<ITaxonomyGroup>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        bool? waitForLoadingNewContent = _waitForLoadingNewContent ? true : null;
        var shouldBypassCache = _waitForLoadingNewContent;

        return _cacheManager is not null && !shouldBypassCache
            ? await ExecuteWithCacheAsync(
                _cacheManager,
                waitForLoadingNewContent,
                cancellationToken).ConfigureAwait(false)
            : await ExecuteWithoutCacheAsync(waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IDeliveryResult<ITaxonomyGroup>> ExecuteWithCacheAsync(
        IDeliveryCacheManager cacheManager,
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildTaxonomyKey(_codename);
        var (cacheResult, apiResult) = await FetchWithCacheAsync(
            cacheManager,
            cacheKey,
            waitForLoadingNewContent,
            cancellationToken).ConfigureAwait(false);

        if (cacheResult.IsCacheHit)
            return DeliveryResult.CacheHit<ITaxonomyGroup>(cacheResult.Value!);

        return apiResult is null ? throw new InvalidOperationException("API result was not captured during fetch.") : apiResult;
    }

    private Task<IDeliveryResult<ITaxonomyGroup>> ExecuteWithoutCacheAsync(
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
        => FetchFromApiAsync(waitForLoadingNewContent, cancellationToken);

    private async Task<(CacheFetchResult<TaxonomyGroup> CacheResult, IDeliveryResult<ITaxonomyGroup>? ApiResult)> FetchWithCacheAsync(
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        IDeliveryResult<ITaxonomyGroup>? apiResult = null;

        var cacheResult = await QueryCacheHelper.GetOrFetchAsync(
            cacheManager,
            cacheKey,
            async ct =>
            {
                apiResult = await FetchFromApiAsync(waitForLoadingNewContent, ct).ConfigureAwait(false);
                if (!apiResult.IsSuccess)
                    return (null, Array.Empty<string>());

                var dependency = CacheDependencyKeyBuilder.BuildTaxonomyDependencyKey(apiResult.Value.System.Codename);
                var dependencies = dependency is null ? Array.Empty<string>() : [dependency];

                return ((TaxonomyGroup)apiResult.Value, dependencies);
            },
            _cacheExpiration,
            logger: null,
            cancellationToken).ConfigureAwait(false);

        return (cacheResult, apiResult);
    }

    private async Task<IDeliveryResult<ITaxonomyGroup>> FetchFromApiAsync(
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var response = await _api.GetTaxonomyInternalAsync(_codename, waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync().ConfigureAwait(false);
    }
}
