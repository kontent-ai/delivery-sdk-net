using System.Diagnostics;
using System.Net;
using Kontent.Ai.Delivery.Api.Filtering;
using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.ContentTypes;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITypesQuery"/>
internal sealed class TypesQuery(
    IDeliveryApi api,
    Func<bool?> getDefaultWaitForNewContent,
    IDeliveryCacheManager? cacheManager,
    ILogger? logger = null) : ITypesQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly List<KeyValuePair<string, string>> _serializedFilters = [];
    private ListTypesParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;
    private readonly ILogger? _logger = logger;

    public ITypesQuery WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public ITypesQuery Skip(int skip)
    {
        _params = _params with { Skip = skip };
        return this;
    }

    public ITypesQuery Limit(int limit)
    {
        _params = _params with { Limit = limit };
        return this;
    }

    public ITypesQuery Where(Func<ITypesFilterBuilder, ITypesFilterBuilder> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        build(new TypesFilterBuilder(_serializedFilters));
        return this;
    }

    public ITypesQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IDeliveryTypeListingResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        LogQueryStarting();
        var stopwatch = StartTimingIfEnabled();

        // 1. CACHE CHECK
        string? cacheKey = null;
        if (_cacheManager != null)
        {
            cacheKey = CacheKeyBuilder.BuildTypesKey(_params, _serializedFilters);
            var cached = await QueryCacheHelper.TryGetCachedAsync<DeliveryTypeListingResponse>(
                _cacheManager, cacheKey, _logger, cancellationToken).ConfigureAwait(false);
            if (cached != null)
            {
                var cachedWithFetcher = cached with { NextPageFetcher = CreateNextPageFetcher(cached.Pagination) };
                LogQueryCompleted(stopwatch, HttpStatusCode.OK, cacheHit: true);
                return DeliveryResult.CacheHit<IDeliveryTypeListingResponse>(cachedWithFetcher);
            }
        }

        // 2. API CALL
        var deliveryResult = await FetchFromApiAsync().ConfigureAwait(false);
        if (!deliveryResult.IsSuccess)
        {
            return DeliveryResult.Failure<IDeliveryTypeListingResponse>(
                deliveryResult.RequestUrl ?? string.Empty,
                deliveryResult.StatusCode,
                deliveryResult.Error,
                deliveryResult.ResponseHeaders);
        }

        // 3. BUILD RESULT WITH NEXT PAGE FETCHER
        var resp = deliveryResult.Value;
        var responseWithFetcher = resp with { NextPageFetcher = CreateNextPageFetcher(resp.Pagination) };
        var result = DeliveryResult.Success<IDeliveryTypeListingResponse>(
            responseWithFetcher,
            deliveryResult.RequestUrl ?? string.Empty,
            deliveryResult.StatusCode,
            deliveryResult.HasStaleContent,
            deliveryResult.ContinuationToken,
            deliveryResult.ResponseHeaders);

        // 4. CACHE & LOG COMPLETION
        // Metadata queries use empty dependencies (rely on TTL for invalidation)
        if (_cacheManager != null && cacheKey != null)
        {
            await QueryCacheHelper.TrySetCachedAsync(
                _cacheManager, cacheKey, resp, dependencies: [], _logger, cancellationToken)
                .ConfigureAwait(false);
        }

        LogQueryCompleted(stopwatch, result.StatusCode, cacheHit: false);
        return result;
    }

    private async Task<IDeliveryResult<DeliveryTypeListingResponse>> FetchFromApiAsync()
    {
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var response = await _api.GetTypesInternalAsync(
            _params,
            FilterQueryParams.ToQueryDictionary(_serializedFilters),
            wait).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync().ConfigureAwait(false);
    }

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryTypeListingResponse>>>? CreateNextPageFetcher(IPagination pagination)
    {
        if (string.IsNullOrEmpty(pagination.NextPageUrl))
            return null;

        var nextSkip = pagination.Skip + pagination.Count;

        return async (ct) =>
        {
            var nextQuery = new TypesQuery(_api, _getDefaultWaitForNewContent, _cacheManager, _logger)
            {
                _params = _params with { Skip = nextSkip },
                _waitForLoadingNewContentOverride = _waitForLoadingNewContentOverride
            };

            foreach (var filter in _serializedFilters)
                nextQuery._serializedFilters.Add(filter);

            return await nextQuery.ExecuteAsync(ct).ConfigureAwait(false);
        };
    }

    private void LogQueryStarting()
    {
        if (_logger != null)
            LoggerMessages.QueryStarting(_logger, "Types", "list");
    }

    private Stopwatch? StartTimingIfEnabled() =>
        _logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

    private void LogQueryCompleted(Stopwatch? stopwatch, HttpStatusCode statusCode, bool cacheHit)
    {
        if (_logger == null) return;
        stopwatch?.Stop();
        LoggerMessages.QueryCompleted(_logger, "Types", "list",
            stopwatch?.ElapsedMilliseconds ?? 0, statusCode, cacheHit);
    }
}