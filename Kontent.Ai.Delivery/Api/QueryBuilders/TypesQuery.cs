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
    private readonly SerializedFilterCollection _serializedFilters = [];
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
        var defaultWaitForNewContent = _getDefaultWaitForNewContent();
        var waitForLoadingNewContent = WaitForLoadingNewContentHelper.ResolveHeaderValue(
            _waitForLoadingNewContentOverride,
            defaultWaitForNewContent);
        var shouldBypassCache = WaitForLoadingNewContentHelper.ShouldBypassCache(
            _waitForLoadingNewContentOverride,
            defaultWaitForNewContent);

        return _cacheManager is not null && !shouldBypassCache
            ? await ExecuteWithCacheAsync(
                _cacheManager,
                stopwatch,
                waitForLoadingNewContent,
                WaitForLoadingNewContentHelper.ResolveCacheMode(_waitForLoadingNewContentOverride, defaultWaitForNewContent),
                cancellationToken).ConfigureAwait(false)
            : await ExecuteWithoutCacheAsync(stopwatch, waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IDeliveryResult<IDeliveryTypeListingResponse>> ExecuteWithCacheAsync(
        IDeliveryCacheManager cacheManager,
        Stopwatch? stopwatch,
        bool? waitForLoadingNewContent,
        string waitCacheMode,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildTypesKey(_params, _serializedFilters, waitCacheMode);
        var (cacheResult, apiResult) = await FetchWithCacheAsync(
            cacheManager,
            cacheKey,
            waitForLoadingNewContent,
            cancellationToken).ConfigureAwait(false);

        if (cacheResult.IsCacheHit)
        {
            LogQueryCompleted(stopwatch, HttpStatusCode.OK, cacheHit: true);
            return DeliveryResult.CacheHit<IDeliveryTypeListingResponse>(
                WithNextPageFetcher(cacheResult.Value!));
        }

        if (apiResult is not { IsSuccess: true })
        {
            if (apiResult is not null)
            {
                LogQueryCompleted(stopwatch, apiResult.StatusCode, cacheHit: false);
                return CreateFailureResult(apiResult);
            }

            throw new InvalidOperationException("API result was not captured during fetch.");
        }

        LogQueryCompleted(stopwatch, apiResult.StatusCode, cacheHit: false);
        return WrapSuccess(WithNextPageFetcher(cacheResult.Value!), apiResult);
    }

    private async Task<IDeliveryResult<IDeliveryTypeListingResponse>> ExecuteWithoutCacheAsync(
        Stopwatch? stopwatch,
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var deliveryResult = await FetchFromApiAsync(waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
        if (!deliveryResult.IsSuccess)
        {
            LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false);
            return CreateFailureResult(deliveryResult);
        }

        LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false);
        return WrapSuccess(WithNextPageFetcher(deliveryResult.Value), deliveryResult);
    }

    private async Task<(CacheFetchResult<DeliveryTypeListingResponse> CacheResult, IDeliveryResult<DeliveryTypeListingResponse>? ApiResult)>
        FetchWithCacheAsync(
            IDeliveryCacheManager cacheManager,
            string cacheKey,
            bool? waitForLoadingNewContent,
            CancellationToken cancellationToken)
    {
        IDeliveryResult<DeliveryTypeListingResponse>? apiResult = null;

        var cacheResult = await QueryCacheHelper.GetOrFetchAsync(
            cacheManager,
            cacheKey,
            async ct =>
            {
                apiResult = await FetchFromApiAsync(waitForLoadingNewContent, ct).ConfigureAwait(false);
                if (!apiResult.IsSuccess)
                    return (null, Array.Empty<string>());

                return (apiResult.Value, Array.Empty<string>());
            },
            _logger,
            cancellationToken).ConfigureAwait(false);

        return (cacheResult, apiResult);
    }

    private async Task<IDeliveryResult<DeliveryTypeListingResponse>> FetchFromApiAsync(
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var response = await _api.GetTypesInternalAsync(
            _params,
            _serializedFilters.ToQueryDictionary(),
            waitForLoadingNewContent,
            cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync(_logger).ConfigureAwait(false);
    }

    private static IDeliveryResult<IDeliveryTypeListingResponse> WrapSuccess(
        DeliveryTypeListingResponse response,
        IDeliveryResult<DeliveryTypeListingResponse> apiResult)
        => DeliveryResult.SuccessFrom<IDeliveryTypeListingResponse, DeliveryTypeListingResponse>(response, apiResult);

    private static IDeliveryResult<IDeliveryTypeListingResponse> CreateFailureResult(
        IDeliveryResult<DeliveryTypeListingResponse> deliveryResult)
        => DeliveryResult.FailureFrom<IDeliveryTypeListingResponse, DeliveryTypeListingResponse>(deliveryResult);

    private DeliveryTypeListingResponse WithNextPageFetcher(DeliveryTypeListingResponse response)
        => response with { NextPageFetcher = CreateNextPageFetcher(response.Pagination) };

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryTypeListingResponse>>>? CreateNextPageFetcher(IPagination pagination)
    {
        if (string.IsNullOrEmpty(pagination.NextPageUrl))
            return null;

        var nextSkip = OffsetPaginationHelper.GetNextSkip(pagination);

        return ct => CreateNextPageQuery(nextSkip).ExecuteAsync(ct);
    }

    private TypesQuery CreateNextPageQuery(int nextSkip)
    {
        var nextQuery = new TypesQuery(_api, _getDefaultWaitForNewContent, _cacheManager, _logger)
        {
            _params = _params with { Skip = nextSkip },
            _waitForLoadingNewContentOverride = _waitForLoadingNewContentOverride
        };

        nextQuery._serializedFilters.CopyFrom(_serializedFilters);
        return nextQuery;
    }

    private void LogQueryStarting()
    {
        if (_logger is not null)
            LoggerMessages.QueryStarting(_logger, "Types", "list");
    }

    private Stopwatch? StartTimingIfEnabled() =>
        _logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

    private void LogQueryCompleted(Stopwatch? stopwatch, HttpStatusCode statusCode, bool cacheHit)
    {
        if (_logger is null)
            return;
        stopwatch?.Stop();
        LoggerMessages.QueryCompleted(_logger, "Types", "list",
            stopwatch?.ElapsedMilliseconds ?? 0, statusCode, cacheHit);
    }
}
