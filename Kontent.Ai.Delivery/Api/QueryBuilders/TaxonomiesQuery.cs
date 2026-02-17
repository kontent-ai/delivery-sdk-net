using System.Diagnostics;
using System.Net;
using Kontent.Ai.Delivery.Api.Filtering;
using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.Logging;
using Kontent.Ai.Delivery.TaxonomyGroups;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITaxonomiesQuery"/>
internal sealed class TaxonomiesQuery(
    IDeliveryApi api,
    IDeliveryCacheManager? cacheManager,
    ILogger? logger = null) : ITaxonomiesQuery, ICacheExpirationConfigurable
{
    private readonly SerializedFilterCollection _serializedFilters = [];
    private ListTaxonomyGroupsParams _params = new();
    private bool _waitForLoadingNewContent;
    public TimeSpan? CacheExpiration { get; set; }

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
        LogQueryStarting();
        var stopwatch = StartTimingIfEnabled();
        bool? waitForLoadingNewContent = _waitForLoadingNewContent ? true : null;
        var shouldBypassCache = _waitForLoadingNewContent;

        return cacheManager is not null && !shouldBypassCache
            ? await ExecuteWithCacheAsync(
                cacheManager,
                stopwatch,
                waitForLoadingNewContent,
                cancellationToken).ConfigureAwait(false)
            : await ExecuteWithoutCacheAsync(stopwatch, waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IDeliveryResult<IDeliveryTaxonomyListingResponse>> ExecuteWithCacheAsync(
        IDeliveryCacheManager cacheManager,
        Stopwatch? stopwatch,
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildTaxonomiesKey(_params, _serializedFilters);
        IDeliveryResult<DeliveryTaxonomyListingResponse>? apiResult = null;

        var cached = await cacheManager.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                apiResult = await FetchFromApiAsync(waitForLoadingNewContent, ct).ConfigureAwait(false);
                if (!apiResult.IsSuccess)
                    return null;

                return new CacheEntry<DeliveryTaxonomyListingResponse>(apiResult.Value, BuildDependencies(apiResult.Value.Taxonomies));
            },
            CacheExpiration,
            cancellationToken).ConfigureAwait(false);

        // Cache hit: apiResult is null because factory was never called
        if (apiResult is null && cached is not null)
        {
            LogQueryCompleted(stopwatch, HttpStatusCode.OK, cacheHit: true);
            return DeliveryResult.CacheHit<IDeliveryTaxonomyListingResponse>(
                WithNextPageFetcher(cached));
        }

        apiResult = QueryExecutionResultHelper.EnsureApiResult(apiResult, "Taxonomies", "list");

        if (!apiResult.IsSuccess)
        {
            LogQueryFailed(apiResult);
            LogQueryCompleted(stopwatch, apiResult.StatusCode, cacheHit: false, apiResult.HasStaleContent);
            return CreateFailureResult(apiResult);
        }

        LogQueryCompleted(stopwatch, apiResult.StatusCode, cacheHit: false, apiResult.HasStaleContent);
        var response = cached ?? apiResult.Value;
        return WrapSuccess(WithNextPageFetcher(response), apiResult);
    }

    private async Task<IDeliveryResult<IDeliveryTaxonomyListingResponse>> ExecuteWithoutCacheAsync(
        Stopwatch? stopwatch,
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var deliveryResult = await FetchFromApiAsync(waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
        if (!deliveryResult.IsSuccess)
        {
            LogQueryFailed(deliveryResult);
            LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false, deliveryResult.HasStaleContent);
            return CreateFailureResult(deliveryResult);
        }

        LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false, deliveryResult.HasStaleContent);
        return WrapSuccess(WithNextPageFetcher(deliveryResult.Value), deliveryResult);
    }

    private async Task<IDeliveryResult<DeliveryTaxonomyListingResponse>> FetchFromApiAsync(
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var response = await api.GetTaxonomiesInternalAsync(
            _params,
            _serializedFilters.ToQueryDictionary(),
            waitForLoadingNewContent,
            cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync(logger).ConfigureAwait(false);
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
        var parametersSnapshot = _params;
        var waitForLoadingSnapshot = _waitForLoadingNewContent;
        var cacheExpirationSnapshot = CacheExpiration;
        var serializedFiltersSnapshot = _serializedFilters.Clone();

        return ct => CreateNextPageQuery(
                nextSkip,
                parametersSnapshot,
                waitForLoadingSnapshot,
                cacheExpirationSnapshot,
                serializedFiltersSnapshot)
            .ExecuteAsync(ct);
    }

    private TaxonomiesQuery CreateNextPageQuery(
        int nextSkip,
        ListTaxonomyGroupsParams parametersSnapshot,
        bool waitForLoadingSnapshot,
        TimeSpan? cacheExpirationSnapshot,
        SerializedFilterCollection serializedFiltersSnapshot)
    {
        var nextQuery = new TaxonomiesQuery(api, cacheManager, logger)
        {
            _params = parametersSnapshot with { Skip = nextSkip },
            _waitForLoadingNewContent = waitForLoadingSnapshot,
            CacheExpiration = cacheExpirationSnapshot
        };

        nextQuery._serializedFilters.CopyFrom(serializedFiltersSnapshot);
        return nextQuery;
    }

    private void LogQueryStarting()
    {
        if (logger is not null)
            LoggerMessages.QueryStarting(logger, "Taxonomies", "list");
    }

    private Stopwatch? StartTimingIfEnabled() =>
        logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

    private void LogQueryFailed(IDeliveryResult<DeliveryTaxonomyListingResponse> deliveryResult)
    {
        if (logger is not null)
        {
            LoggerMessages.QueryFailed(logger, "Taxonomies", "list", deliveryResult.StatusCode,
                deliveryResult.Error?.Message, exception: null);
        }
    }

    private void LogQueryCompleted(Stopwatch? stopwatch, HttpStatusCode statusCode, bool cacheHit, bool hasStaleContent = false)
    {
        if (logger is null)
            return;
        stopwatch?.Stop();
        if (hasStaleContent)
            LoggerMessages.QueryStaleContent(logger, "list");
        LoggerMessages.QueryCompleted(logger, "Taxonomies", "list",
            stopwatch?.ElapsedMilliseconds ?? 0, statusCode, cacheHit);
    }
}
