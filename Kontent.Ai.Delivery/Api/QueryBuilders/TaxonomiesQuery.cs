using Kontent.Ai.Delivery.Api.Filtering;
using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.Extensions;
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
        // 1. CACHE CHECK
        string? cacheKey = null;
        if (_cacheManager != null)
        {
            cacheKey = CacheKeyBuilder.BuildTaxonomiesKey(_params, _serializedFilters);
            var cached = await QueryCacheHelper.TryGetCachedAsync<DeliveryTaxonomyListingResponse>(
                _cacheManager, cacheKey, logger: null, cancellationToken).ConfigureAwait(false);
            if (cached != null)
            {
                var cachedWithFetcher = cached with { NextPageFetcher = CreateNextPageFetcher(cached.Pagination) };
                return DeliveryResult.CacheHit<IDeliveryTaxonomyListingResponse>(cachedWithFetcher);
            }
        }

        // 2. API CALL
        var deliveryResult = await FetchFromApiAsync().ConfigureAwait(false);
        if (!deliveryResult.IsSuccess)
        {
            return DeliveryResult.Failure<IDeliveryTaxonomyListingResponse>(
                deliveryResult.RequestUrl ?? string.Empty,
                deliveryResult.StatusCode,
                deliveryResult.Error,
                deliveryResult.ResponseHeaders);
        }

        // 3. BUILD RESULT WITH NEXT PAGE FETCHER
        var resp = deliveryResult.Value;
        var responseWithFetcher = resp with { NextPageFetcher = CreateNextPageFetcher(resp.Pagination) };
        var result = DeliveryResult.Success<IDeliveryTaxonomyListingResponse>(
            responseWithFetcher,
            deliveryResult.RequestUrl ?? string.Empty,
            deliveryResult.StatusCode,
            deliveryResult.HasStaleContent,
            deliveryResult.ContinuationToken,
            deliveryResult.ResponseHeaders);

        // 4. CACHE RESULT
        // Metadata queries use empty dependencies (rely on TTL for invalidation)
        if (_cacheManager != null && cacheKey != null)
        {
            await QueryCacheHelper.TrySetCachedAsync(
                _cacheManager, cacheKey, resp, dependencies: [], logger: null, cancellationToken)
                .ConfigureAwait(false);
        }

        return result;
    }

    private async Task<IDeliveryResult<DeliveryTaxonomyListingResponse>> FetchFromApiAsync()
    {
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var response = await _api.GetTaxonomiesInternalAsync(
            _params,
            FilterQueryParams.ToQueryDictionary(_serializedFilters),
            wait).ConfigureAwait(false);
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