using Kontent.Ai.Delivery.Api.Filtering;
using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.TaxonomyGroups;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITaxonomiesQuery"/>
internal sealed class TaxonomiesQuery(
    IDeliveryApi api,
    IDeliveryCacheManager? cacheManager) : ITaxonomiesQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly SerializedFilterCollection _serializedFilters = [];
    private ListTaxonomyGroupsParams _params = new();
    private bool _waitForLoadingNewContent;
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
        _waitForLoadingNewContent = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IDeliveryTaxonomyListingResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
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

    private async Task<IDeliveryResult<IDeliveryTaxonomyListingResponse>> ExecuteWithCacheAsync(
        IDeliveryCacheManager cacheManager,
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildTaxonomiesKey(_params, _serializedFilters);
        var (cacheResult, apiResult) = await FetchWithCacheAsync(
            cacheManager,
            cacheKey,
            waitForLoadingNewContent,
            cancellationToken).ConfigureAwait(false);

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

    private async Task<IDeliveryResult<IDeliveryTaxonomyListingResponse>> ExecuteWithoutCacheAsync(
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var deliveryResult = await FetchFromApiAsync(waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
        return deliveryResult.IsSuccess
            ? WrapSuccess(WithNextPageFetcher(deliveryResult.Value), deliveryResult)
            : CreateFailureResult(deliveryResult);
    }

    private async Task<(CacheFetchResult<DeliveryTaxonomyListingResponse> CacheResult, IDeliveryResult<DeliveryTaxonomyListingResponse>? ApiResult)>
        FetchWithCacheAsync(
            IDeliveryCacheManager cacheManager,
            string cacheKey,
            bool? waitForLoadingNewContent,
            CancellationToken cancellationToken)
    {
        IDeliveryResult<DeliveryTaxonomyListingResponse>? apiResult = null;

        var cacheResult = await QueryCacheHelper.GetOrFetchAsync(
            cacheManager,
            cacheKey,
            async ct =>
            {
                apiResult = await FetchFromApiAsync(waitForLoadingNewContent, ct).ConfigureAwait(false);
                if (!apiResult.IsSuccess)
                    return (null, Array.Empty<string>());

                return (apiResult.Value, BuildDependencies(apiResult.Value.Taxonomies));
            },
            logger: null,
            cancellationToken).ConfigureAwait(false);

        return (cacheResult, apiResult);
    }

    private async Task<IDeliveryResult<DeliveryTaxonomyListingResponse>> FetchFromApiAsync(
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var response = await _api.GetTaxonomiesInternalAsync(
            _params,
            _serializedFilters.ToQueryDictionary(),
            waitForLoadingNewContent,
            cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync().ConfigureAwait(false);
    }

    private static string[] BuildDependencies(IReadOnlyList<TaxonomyGroup> taxonomies)
    {
        var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            DeliveryCacheDependencies.TaxonomiesListScope
        };

        foreach (var taxonomy in taxonomies)
        {
            var dependency = CacheDependencyKeyBuilder.BuildTaxonomyDependencyKey(taxonomy.System.Codename);
            if (dependency is null)
                continue;

            dependencies.Add(dependency);
        }

        return [.. dependencies];
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
        var nextQuery = new TaxonomiesQuery(_api, _cacheManager)
        {
            _params = _params with { Skip = nextSkip },
            _waitForLoadingNewContent = this._waitForLoadingNewContent
        };

        nextQuery._serializedFilters.CopyFrom(_serializedFilters);
        return nextQuery;
    }
}
