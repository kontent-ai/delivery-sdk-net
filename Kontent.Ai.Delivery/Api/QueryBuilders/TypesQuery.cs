using System.Diagnostics;
using System.Net;
using Kontent.Ai.Delivery.Api.Filtering;
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
        // Log query starting
        if (_logger != null)
            LoggerMessages.QueryStarting(_logger, "Types", "list");

        // Start timing if logging is enabled
        var stopwatch = _logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

        // Cache check (if enabled)
        string? cacheKey = null;
        if (_cacheManager != null)
        {
            try
            {
                cacheKey = CacheKeyBuilder.BuildTypesKey(_params, _serializedFilters);
                var cached = await _cacheManager.GetAsync<DeliveryTypeListingResponse>(cacheKey, cancellationToken)
                    .ConfigureAwait(false);

                if (cached != null)
                {
                    // Log cache hit
                    if (_logger != null)
                    {
                        LoggerMessages.QueryCacheHit(_logger, cacheKey);
                        LoggerMessages.QueryCompleted(_logger, "Types", "list",
                            stopwatch?.ElapsedMilliseconds ?? 0, HttpStatusCode.OK, cacheHit: true);
                    }

                    // Reconstruct with NextPageFetcher
                    var cachedWithFetcher = cached with
                    {
                        NextPageFetcher = CreateNextPageFetcher(cached.Pagination)
                    };

                    return DeliveryResult.CacheHit<IDeliveryTypeListingResponse>(cachedWithFetcher);
                }

                // Log cache miss
                if (_logger != null)
                    LoggerMessages.QueryCacheMiss(_logger, cacheKey);
            }
            catch (Exception ex)
            {
                // Cache read failed - continue with API call
                if (_logger != null && cacheKey != null)
                    LoggerMessages.CacheGetFailed(_logger, cacheKey, ex);
            }
        }

        // API call
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var response = await _api.GetTypesInternalAsync(
            _params,
            FilterQueryParams.ToQueryDictionary(_serializedFilters),
            wait).ConfigureAwait(false);
        var deliveryResult = await response.ToDeliveryResultAsync().ConfigureAwait(false);

        if (!deliveryResult.IsSuccess)
        {
            return DeliveryResult.Failure<IDeliveryTypeListingResponse>(
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

        var result = DeliveryResult.Success<IDeliveryTypeListingResponse>(
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
            catch (Exception ex)
            {
                // Cache write failed - still return result
                if (_logger != null)
                    LoggerMessages.CacheSetFailed(_logger, cacheKey, ex);
            }
        }

        // Log completion
        if (_logger != null)
        {
            stopwatch?.Stop();
            LoggerMessages.QueryCompleted(_logger, "Types", "list",
                stopwatch?.ElapsedMilliseconds ?? 0, result.StatusCode, cacheHit: false);
        }

        return result;
    }

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryTypeListingResponse>>>? CreateNextPageFetcher(IPagination pagination)
    {
        if (string.IsNullOrEmpty(pagination.NextPageUrl))
            return null;

        // Calculate next skip value
        var nextSkip = pagination.Skip + pagination.Count;

        return async (ct) =>
        {
            var nextQuery = new TypesQuery(_api, _getDefaultWaitForNewContent, _cacheManager, _logger)
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