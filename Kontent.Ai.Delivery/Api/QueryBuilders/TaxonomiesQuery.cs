using Kontent.Ai.Delivery.Api.Filtering;
using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.TaxonomyGroups;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITaxonomiesQuery"/>
internal sealed class TaxonomiesQuery(
    IDeliveryApi api,
    Func<bool?> getDefaultWaitForNewContent,
    IDeliveryCacheManager? cacheManager) : ITaxonomiesQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly List<KeyValuePair<string, string>> _serializedFilters = [];
    private ListTaxonomyGroupsParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;

    public ITaxonomiesQuery Skip(int skip)
    {
        _params = _params with { Skip = skip };
        return this;
    }

    public ITaxonomiesQuery Limit(int limit)
    {
        _params = _params with { Limit = limit };
        return this;
    }

    public ITaxonomiesQuery Where(Func<ITaxonomiesFilterBuilder, ITaxonomiesFilterBuilder> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        build(new TaxonomiesFilterBuilder(_serializedFilters));
        return this;
    }

    public ITaxonomiesQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IDeliveryTaxonomyListingResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_cacheManager != null)
        {
            var cacheKey = CacheKeyBuilder.BuildTaxonomiesKey(_params, _serializedFilters);
            IDeliveryResult<DeliveryTaxonomyListingResponse>? apiResult = null;

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
                    return (apiResult.Value, Array.Empty<string>());
                },
                logger: null,
                cancellationToken).ConfigureAwait(false);

            if (cacheResult.IsCacheHit)
            {
                // Reconstruct with NextPageFetcher (delegates can't be cached)
                var cachedWithFetcher = cacheResult.Value! with { NextPageFetcher = CreateNextPageFetcher(cacheResult.Value!.Pagination) };
                return DeliveryResult.CacheHit<IDeliveryTaxonomyListingResponse>(cachedWithFetcher);
            }

            // Not a cache hit - check if API succeeded
            if (!apiResult!.IsSuccess)
            {
                return DeliveryResult.Failure<IDeliveryTaxonomyListingResponse>(
                    apiResult.RequestUrl ?? string.Empty,
                    apiResult.StatusCode,
                    apiResult.Error,
                    apiResult.ResponseHeaders);
            }

            // Fresh fetch succeeded - build result with fetcher
            var responseWithFetcher = cacheResult.Value! with { NextPageFetcher = CreateNextPageFetcher(cacheResult.Value!.Pagination) };
            return DeliveryResult.Success<IDeliveryTaxonomyListingResponse>(
                responseWithFetcher,
                apiResult.RequestUrl ?? string.Empty,
                apiResult.StatusCode,
                apiResult.HasStaleContent,
                apiResult.ContinuationToken,
                apiResult.ResponseHeaders);
        }

        // No caching - direct fetch
        var deliveryResult = await FetchFromApiAsync(cancellationToken).ConfigureAwait(false);
        if (!deliveryResult.IsSuccess)
        {
            return DeliveryResult.Failure<IDeliveryTaxonomyListingResponse>(
                deliveryResult.RequestUrl ?? string.Empty,
                deliveryResult.StatusCode,
                deliveryResult.Error,
                deliveryResult.ResponseHeaders);
        }

        var resp = deliveryResult.Value;
        var respWithFetcher = resp with { NextPageFetcher = CreateNextPageFetcher(resp.Pagination) };
        return DeliveryResult.Success<IDeliveryTaxonomyListingResponse>(
            respWithFetcher,
            deliveryResult.RequestUrl ?? string.Empty,
            deliveryResult.StatusCode,
            deliveryResult.HasStaleContent,
            deliveryResult.ContinuationToken,
            deliveryResult.ResponseHeaders);
    }

    private async Task<IDeliveryResult<DeliveryTaxonomyListingResponse>> FetchFromApiAsync(CancellationToken cancellationToken)
    {
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var response = await _api.GetTaxonomiesInternalAsync(
            _params,
            FilterQueryParams.ToQueryDictionary(_serializedFilters),
            wait,
            cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync().ConfigureAwait(false);
    }

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryTaxonomyListingResponse>>>? CreateNextPageFetcher(IPagination pagination)
    {
        if (string.IsNullOrEmpty(pagination.NextPageUrl))
            return null;

        var nextSkip = pagination.Skip + pagination.Count;

        return async (ct) =>
        {
            var nextQuery = new TaxonomiesQuery(_api, _getDefaultWaitForNewContent, _cacheManager)
            {
                _params = _params with { Skip = nextSkip },
                _waitForLoadingNewContentOverride = _waitForLoadingNewContentOverride
            };

            foreach (var filter in _serializedFilters)
                nextQuery._serializedFilters.Add(filter);

            return await nextQuery.ExecuteAsync(ct).ConfigureAwait(false);
        };
    }
}
