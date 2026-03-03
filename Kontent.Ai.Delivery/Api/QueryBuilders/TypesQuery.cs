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
    IDeliveryCacheManager? cacheManager,
    ILogger? logger = null) : ITypesQuery, ICacheExpirationConfigurable
{
    private readonly SerializedFilterCollection _serializedFilters = [];
    private ListTypesParams _params = new();
    private bool _waitForLoadingNewContent;
    public TimeSpan? CacheExpiration { get; set; }

    public ITypesQuery WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = string.Join(",", elementCodenames) };
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
        _waitForLoadingNewContent = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IDeliveryTypeListingResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
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

    private async Task<IDeliveryResult<IDeliveryTypeListingResponse>> ExecuteWithCacheAsync(
        IDeliveryCacheManager cacheManager,
        Stopwatch? stopwatch,
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildTypesKey(_params, _serializedFilters);
        IDeliveryResult<DeliveryTypeListingResponse>? apiResult = null;
        var factoryInvoked = false;

        var cached = await cacheManager.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                factoryInvoked = true;
                apiResult = await FetchFromApiAsync(waitForLoadingNewContent, ct).ConfigureAwait(false);
                if (!apiResult.IsSuccess)
                    return null;

                return new CacheEntry<DeliveryTypeListingResponse>(apiResult.Value, BuildDependencies(apiResult.Value.Types));
            },
            CacheExpiration,
            cancellationToken).ConfigureAwait(false);

        // Cache hit (factory never called) or fail-safe served stale data after HTTP error
        if (cached is not null && (apiResult is null || !apiResult.IsSuccess))
        {
            LogQueryCompleted(stopwatch, HttpStatusCode.OK, cacheHit: true);
            var isFailSafe = factoryInvoked
                || (cacheManager is IFailSafeStateProvider failSafeProvider && failSafeProvider.IsFailSafeActive(cacheKey));

            return isFailSafe
                ? DeliveryResult.FailSafeHit<IDeliveryTypeListingResponse>(WithNextPageFetcher(cached))
                : DeliveryResult.CacheHit<IDeliveryTypeListingResponse>(WithNextPageFetcher(cached));
        }

        apiResult = QueryExecutionResultHelper.EnsureApiResult(apiResult, "Types", "list");

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

    private async Task<IDeliveryResult<IDeliveryTypeListingResponse>> ExecuteWithoutCacheAsync(
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

    private async Task<IDeliveryResult<DeliveryTypeListingResponse>> FetchFromApiAsync(
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var response = await api.GetTypesInternalAsync(
            _params,
            _serializedFilters.ToQueryDictionary(),
            waitForLoadingNewContent,
            cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync(logger).ConfigureAwait(false);
    }

    private static string[] BuildDependencies(IReadOnlyList<ContentType> types)
    {
        var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            DeliveryCacheDependencies.TypesListScope
        };

        foreach (var type in types)
        {
            var dependency = CacheDependencyKeyBuilder.BuildTypeDependencyKey(type.System.Codename);
            if (dependency is null)
                continue;

            dependencies.Add(dependency);
        }

        return [.. dependencies];
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

    private TypesQuery CreateNextPageQuery(
        int nextSkip,
        ListTypesParams parametersSnapshot,
        bool waitForLoadingSnapshot,
        TimeSpan? cacheExpirationSnapshot,
        SerializedFilterCollection serializedFiltersSnapshot)
    {
        var nextQuery = new TypesQuery(api, cacheManager, logger)
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
            LoggerMessages.QueryStarting(logger, "Types", "list");
    }

    private Stopwatch? StartTimingIfEnabled() =>
        logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

    private void LogQueryFailed(IDeliveryResult<DeliveryTypeListingResponse> deliveryResult)
    {
        if (logger is not null)
        {
            LoggerMessages.QueryFailed(logger, "Types", "list", deliveryResult.StatusCode,
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
        LoggerMessages.QueryCompleted(logger, "Types", "list",
            stopwatch?.ElapsedMilliseconds ?? 0, statusCode, cacheHit);
    }
}
