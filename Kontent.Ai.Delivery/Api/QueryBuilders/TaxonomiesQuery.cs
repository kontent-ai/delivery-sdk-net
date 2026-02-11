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
    private readonly SerializedFilterCollection _serializedFilters = [];
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
        return _cacheManager is not null
            ? await ExecuteWithCacheAsync(_cacheManager, cancellationToken).ConfigureAwait(false)
            : await ExecuteWithoutCacheAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IDeliveryResult<IDeliveryTaxonomyListingResponse>> ExecuteWithCacheAsync(
        IDeliveryCacheManager cacheManager,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildTaxonomiesKey(_params, _serializedFilters);
        var (cacheResult, apiResult) = await FetchWithCacheAsync(cacheManager, cacheKey, cancellationToken).ConfigureAwait(false);

        if (cacheResult.IsCacheHit)
        {
            return DeliveryResult.CacheHit<IDeliveryTaxonomyListingResponse>(
                WithNextPageFetcher(cacheResult.Value!));
        }

        if (apiResult is not { IsSuccess: true })
        {
            if (apiResult is not null)
                return CreateFailureResult(apiResult);

            throw new InvalidOperationException("API result was not captured during fetch.");
        }

        return WrapSuccess(WithNextPageFetcher(cacheResult.Value!), apiResult);
    }

    private async Task<IDeliveryResult<IDeliveryTaxonomyListingResponse>> ExecuteWithoutCacheAsync(CancellationToken cancellationToken)
    {
        var deliveryResult = await FetchFromApiAsync(cancellationToken).ConfigureAwait(false);
        return deliveryResult.IsSuccess
            ? WrapSuccess(WithNextPageFetcher(deliveryResult.Value), deliveryResult)
            : CreateFailureResult(deliveryResult);
    }

    private async Task<(CacheFetchResult<DeliveryTaxonomyListingResponse> CacheResult, IDeliveryResult<DeliveryTaxonomyListingResponse>? ApiResult)>
        FetchWithCacheAsync(IDeliveryCacheManager cacheManager, string cacheKey, CancellationToken cancellationToken)
    {
        IDeliveryResult<DeliveryTaxonomyListingResponse>? apiResult = null;

        var cacheResult = await QueryCacheHelper.GetOrFetchAsync(
            cacheManager,
            cacheKey,
            async ct =>
            {
                apiResult = await FetchFromApiAsync(ct).ConfigureAwait(false);
                if (!apiResult.IsSuccess)
                    return (null, Array.Empty<string>());

                return (apiResult.Value, Array.Empty<string>());
            },
            logger: null,
            cancellationToken).ConfigureAwait(false);

        return (cacheResult, apiResult);
    }

    private async Task<IDeliveryResult<DeliveryTaxonomyListingResponse>> FetchFromApiAsync(CancellationToken cancellationToken)
    {
        var wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var response = await _api.GetTaxonomiesInternalAsync(
            _params,
            _serializedFilters.ToQueryDictionary(),
            wait,
            cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync().ConfigureAwait(false);
    }

    private static IDeliveryResult<IDeliveryTaxonomyListingResponse> WrapSuccess(
        DeliveryTaxonomyListingResponse response,
        IDeliveryResult<DeliveryTaxonomyListingResponse> apiResult)
        => DeliveryResult.SuccessFrom<IDeliveryTaxonomyListingResponse, DeliveryTaxonomyListingResponse>(response, apiResult);

    private static IDeliveryResult<IDeliveryTaxonomyListingResponse> CreateFailureResult(
        IDeliveryResult<DeliveryTaxonomyListingResponse> deliveryResult)
        => DeliveryResult.FailureFrom<IDeliveryTaxonomyListingResponse, DeliveryTaxonomyListingResponse>(deliveryResult);

    private DeliveryTaxonomyListingResponse WithNextPageFetcher(DeliveryTaxonomyListingResponse response)
        => response with { NextPageFetcher = CreateNextPageFetcher(response.Pagination) };

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryTaxonomyListingResponse>>>? CreateNextPageFetcher(IPagination pagination)
    {
        if (string.IsNullOrEmpty(pagination.NextPageUrl))
            return null;

        var nextSkip = OffsetPaginationHelper.GetNextSkip(pagination);

        return ct => CreateNextPageQuery(nextSkip).ExecuteAsync(ct);
    }

    private TaxonomiesQuery CreateNextPageQuery(int nextSkip)
    {
        var nextQuery = new TaxonomiesQuery(_api, _getDefaultWaitForNewContent, _cacheManager)
        {
            _params = _params with { Skip = nextSkip },
            _waitForLoadingNewContentOverride = _waitForLoadingNewContentOverride
        };

        nextQuery._serializedFilters.CopyFrom(_serializedFilters);
        return nextQuery;
    }
}
