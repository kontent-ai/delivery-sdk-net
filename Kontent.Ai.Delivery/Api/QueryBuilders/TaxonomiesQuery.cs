using System.Net;
using Kontent.Ai.Delivery.Api.Filtering;
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
        // Cache check (if enabled)
        string? cacheKey = null;
        if (_cacheManager != null)
        {
            try
            {
                cacheKey = CacheKeyBuilder.BuildTaxonomiesKey(_params, _serializedFilters);
                var cached = await _cacheManager.GetAsync<DeliveryTaxonomyListingResponse>(cacheKey, cancellationToken)
                    .ConfigureAwait(false);

                if (cached != null)
                {
                    // Reconstruct with NextPageFetcher
                    var cachedWithFetcher = cached with
                    {
                        NextPageFetcher = CreateNextPageFetcher(cached.Pagination)
                    };

                    return DeliveryResult.CacheHit<IDeliveryTaxonomyListingResponse>(cachedWithFetcher);
                }
            }
            catch (Exception)
            {
                // Cache read failed - continue with API call
            }
        }

        // API call
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var response = await _api.GetTaxonomiesInternalAsync(
            _params,
            FilterQueryParams.ToQueryDictionary(_serializedFilters),
            wait).ConfigureAwait(false);
        var deliveryResult = await response.ToDeliveryResultAsync().ConfigureAwait(false);

        if (!deliveryResult.IsSuccess)
        {
            return DeliveryResult.Failure<IDeliveryTaxonomyListingResponse>(
                deliveryResult.RequestUrl ?? string.Empty,
                deliveryResult.StatusCode,
                deliveryResult.Error,
                deliveryResult.ResponseHeaders);
        }

        var resp = deliveryResult.Value;

        // Build response with next page fetcher
        var responseWithFetcher = resp with
        {
            NextPageFetcher = CreateNextPageFetcher(resp.Pagination)
        };

        var result = DeliveryResult.Success<IDeliveryTaxonomyListingResponse>(
            responseWithFetcher,
            deliveryResult.RequestUrl ?? string.Empty,
            deliveryResult.StatusCode,
            deliveryResult.HasStaleContent,
            deliveryResult.ContinuationToken,
            deliveryResult.ResponseHeaders);

        // Cache result (if enabled) - metadata queries use empty dependencies (rely on TTL for invalidation)
        if (_cacheManager != null && cacheKey != null)
        {
            try
            {
                // Cache response without NextPageFetcher (delegates can't be serialized)
                await _cacheManager.SetAsync(
                    cacheKey,
                    resp,
                    dependencies: [], // Metadata queries don't track dependencies
                    expiration: null,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Cache write failed - still return result
            }
        }

        return result;
    }

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryTaxonomyListingResponse>>>? CreateNextPageFetcher(IPagination pagination)
    {
        if (string.IsNullOrEmpty(pagination.NextPageUrl))
            return null;

        // Calculate next skip value
        var nextSkip = pagination.Skip + pagination.Count;

        return async (ct) =>
        {
            var nextQuery = new TaxonomiesQuery(_api, _getDefaultWaitForNewContent, _cacheManager)
            {
                _params = _params with { Skip = nextSkip },
                _waitForLoadingNewContentOverride = _waitForLoadingNewContentOverride
            };

            // Copy filters
            foreach (var filter in _serializedFilters)
            {
                nextQuery._serializedFilters.Add(filter);
            }

            return await nextQuery.ExecuteAsync(ct).ConfigureAwait(false);
        };
    }
}