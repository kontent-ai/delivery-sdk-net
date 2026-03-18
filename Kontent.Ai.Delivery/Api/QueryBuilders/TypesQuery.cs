using System.Diagnostics;
using System.Net;
using Kontent.Ai.Delivery.Api.Filtering;
using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.ContentTypes;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITypesQuery"/>
internal sealed class TypesQuery(
    IDeliveryApi api,
    IDeliveryCacheManager? cacheManager,
    ILogger? logger = null) : ITypesQuery, ICacheExpirationConfigurable
{
    private readonly QueryLoggingHelper _log = new(logger, "Types", "list");
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
        _log.LogQueryStarting();
        var stopwatch = _log.StartTimingIfEnabled();
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

                var dependencies = BuildDependencies(apiResult.Value.Types);
                return new CacheEntry<DeliveryTypeListingResponse>(apiResult.Value, dependencies);
            },
            CacheExpiration,
            cancellationToken).ConfigureAwait(false);

        // Cache hit (factory never called) or fail-safe served stale data after HTTP error
        if (cached is not null && (apiResult is null || !apiResult.IsSuccess))
        {
            _log.LogQueryCompleted(stopwatch, HttpStatusCode.OK, cacheHit: true);
            var isFailSafe = factoryInvoked
                || (cacheManager is IFailSafeStateProvider failSafeProvider && failSafeProvider.IsFailSafeActive(cacheKey));

            return isFailSafe
                ? DeliveryResult.FailSafeHit<IDeliveryTypeListingResponse>(
                    WithNextPageFetcher(cached.Value), cached.DependencyKeys)
                : DeliveryResult.CacheHit<IDeliveryTypeListingResponse>(
                    WithNextPageFetcher(cached.Value), cached.DependencyKeys);
        }

        apiResult = QueryExecutionResultHelper.EnsureApiResult(apiResult, "Types", "list");

        if (!apiResult.IsSuccess)
        {
            _log.LogQueryFailed(apiResult.StatusCode, apiResult.Error?.Message);
            _log.LogQueryCompleted(stopwatch, apiResult.StatusCode, cacheHit: false, apiResult.HasStaleContent);
            return CreateFailureResult(apiResult);
        }

        _log.LogQueryCompleted(stopwatch, apiResult.StatusCode, cacheHit: false, apiResult.HasStaleContent);
        var response = cached?.Value ?? apiResult.Value;
        return WrapSuccess(
            WithNextPageFetcher(response),
            apiResult,
            cached?.DependencyKeys ?? BuildDependencies(response.Types));
    }

    private async Task<IDeliveryResult<IDeliveryTypeListingResponse>> ExecuteWithoutCacheAsync(
        Stopwatch? stopwatch,
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var deliveryResult = await FetchFromApiAsync(waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
        if (!deliveryResult.IsSuccess)
        {
            _log.LogQueryFailed(deliveryResult.StatusCode, deliveryResult.Error?.Message);
            _log.LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false, deliveryResult.HasStaleContent);
            return CreateFailureResult(deliveryResult);
        }

        _log.LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false, deliveryResult.HasStaleContent);
        return WrapSuccess(
            WithNextPageFetcher(deliveryResult.Value),
            deliveryResult,
            BuildDependencies(deliveryResult.Value.Types));
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
        IDeliveryResult<DeliveryTypeListingResponse> apiResult,
        IReadOnlyList<string> dependencyKeys)
        => DeliveryResult.SuccessFrom<IDeliveryTypeListingResponse, DeliveryTypeListingResponse>(
            response, apiResult, dependencyKeys);

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

}
