using System.Diagnostics;
using System.Net;
using Kontent.Ai.Delivery.Api.Filtering;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IItemsQuery{TModel}"/>
internal sealed class ItemsQuery<TModel>(
    IDeliveryApi api,
    Func<bool?> getDefaultWaitForNewContent,
    ContentItemMapper contentItemMapper,
    IDeliveryCacheManager? cacheManager,
    ILogger? logger = null) : IItemsQuery<TModel>
{
    private readonly IDeliveryApi _api = api;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly ContentItemMapper _contentItemMapper = contentItemMapper;
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;
    private readonly ILogger? _logger = logger;
    private readonly List<KeyValuePair<string, string>> _serializedFilters = [];
    private ListItemsParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private static bool IsDynamicModel => typeof(TModel) == typeof(IDynamicElements) || typeof(TModel) == typeof(DynamicElements);

    public IItemsQuery<TModel> WithLanguage(string languageCodename, LanguageFallbackMode languageFallbackMode = LanguageFallbackMode.Enabled)
    {
        _params = _params with { Language = languageCodename };
        if (languageFallbackMode == LanguageFallbackMode.Disabled)
        {
            _serializedFilters.Add(new KeyValuePair<string, string>(
                FilterPath.System("language") + FilterSuffix.Eq,
                FilterValueSerializer.Serialize(languageCodename)));
        }
        return this;
    }

    public IItemsQuery<TModel> WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public IItemsQuery<TModel> WithoutElements(params string[] elementCodenames)
    {
        _params = _params with { ExcludeElements = elementCodenames };
        return this;
    }

    public IItemsQuery<TModel> Depth(int depth)
    {
        _params = _params with { Depth = depth };
        return this;
    }

    public IItemsQuery<TModel> Skip(int skip)
    {
        _params = _params with { Skip = skip };
        return this;
    }

    public IItemsQuery<TModel> Limit(int limit)
    {
        _params = _params with { Limit = limit };
        return this;
    }

    public IItemsQuery<TModel> OrderBy(string elementOrAttributePath, OrderingMode orderingMode = OrderingMode.Ascending)
    {
        _params = _params with
        {
            OrderBy = orderingMode == OrderingMode.Ascending
                ? $"{elementOrAttributePath}[asc]"
                : $"{elementOrAttributePath}[desc]"
        };
        return this;
    }

    public IItemsQuery<TModel> WithTotalCount()
    {
        _params = _params with { IncludeTotalCount = true };
        return this;
    }

    public IItemsQuery<TModel> WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public IItemsQuery<TModel> Where(Func<IItemsFilterBuilder, IItemsFilterBuilder> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        build(new ItemsFilterBuilder(_serializedFilters));
        return this;
    }

    public async Task<IDeliveryResult<IDeliveryItemListingResponse<TModel>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Log query starting
        if (_logger != null)
            LoggerMessages.QueryStarting(_logger, "Items", "list");

        // Start timing if logging is enabled
        var stopwatch = _logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

        // ========== 1. CACHE CHECK (if enabled) ==========
        string? cacheKey = null;
        if (_cacheManager != null)
        {
            try
            {
                cacheKey = CacheKeyBuilder.BuildItemsKey(_params, _serializedFilters);
                // Cache the response (without NextPageFetcher - we'll add it after)
                var cachedResponse = await _cacheManager.GetAsync<DeliveryItemListingResponse<TModel>>(cacheKey, cancellationToken)
                    .ConfigureAwait(false);

                if (cachedResponse != null)
                {
                    // Reconstruct with NextPageFetcher (delegates can't be cached)
                    var cachedResponseWithFetcher = cachedResponse with
                    {
                        NextPageFetcher = CreateNextPageFetcher(cachedResponse.Pagination)
                    };

                    var cachedResult = DeliveryResult.CacheHit<IDeliveryItemListingResponse<TModel>>(cachedResponseWithFetcher);

                    // Log cache hit and return
                    if (_logger != null)
                    {
                        LoggerMessages.QueryCacheHit(_logger, cacheKey);
                        LoggerMessages.QueryCompleted(_logger, "Items", "list",
                            stopwatch?.ElapsedMilliseconds ?? 0, HttpStatusCode.OK, cacheHit: true);
                    }
                    return cachedResult;
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

        // ========== 2. API CALL (cache miss or disabled) ==========
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var rawResponse = await _api.GetItemsInternalAsync<TModel>(
            _params,
            FilterQueryParams.ToQueryDictionary(_serializedFilters),
            wait).ConfigureAwait(false);

        // Convert IApiResponse to IDeliveryResult
        var deliveryResult = await rawResponse.ToDeliveryResultAsync().ConfigureAwait(false);

        if (!deliveryResult.IsSuccess)
        {
            // Log query failure
            if (_logger != null)
                LoggerMessages.QueryFailed(_logger, "Items", "list", deliveryResult.StatusCode,
                    deliveryResult.Error?.Message, exception: null);

            return DeliveryResult.Failure<IDeliveryItemListingResponse<TModel>>(
                deliveryResult.RequestUrl ?? string.Empty,
                deliveryResult.StatusCode,
                deliveryResult.Error,
                deliveryResult.ResponseHeaders);
        }

        // ========== 3. POST-PROCESS WITH DEPENDENCY TRACKING ==========
        var resp = deliveryResult.Value;
        var items = resp.Items;

        // Create dependency context only if caching enabled
        var dependencyContext = _cacheManager != null ? new DependencyTrackingContext() : null;

        // Track all items in the response
        if (dependencyContext != null && items is { Count: > 0 })
        {
            foreach (var item in items)
            {
                dependencyContext.TrackItem(item.System.Codename);
            }
        }

        // Track modular content dependencies
        if (dependencyContext != null && resp.ModularContent != null)
        {
            foreach (var codename in resp.ModularContent.Keys)
            {
                dependencyContext.TrackItem(codename);
            }
        }

        // Post-process each item (will track additional dependencies: assets, taxonomies)
        // Dynamic mode intentionally stays raw (no hydration).
        if (!IsDynamicModel)
        {
            foreach (var item in items)
            {
                await _contentItemMapper.CompleteItemAsync(item, resp.ModularContent, dependencyContext, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        // ========== 4. BUILD RESULT WITH NEXT PAGE FETCHER ==========
        var responseWithFetcher = resp with
        {
            NextPageFetcher = CreateNextPageFetcher(resp.Pagination)
        };

        var result = DeliveryResult.Success<IDeliveryItemListingResponse<TModel>>(
            responseWithFetcher,
            deliveryResult.RequestUrl ?? string.Empty,
            deliveryResult.StatusCode,
            deliveryResult.HasStaleContent,
            deliveryResult.ContinuationToken,
            deliveryResult.ResponseHeaders);

        // ========== 5. CACHE RESULT (if enabled) ==========
        if (_cacheManager != null && dependencyContext != null && cacheKey != null)
        {
            try
            {
                // Cache response without NextPageFetcher (delegates can't be serialized)
                await _cacheManager.SetAsync(
                    cacheKey,
                    resp, // Original response without NextPageFetcher
                    dependencyContext.Dependencies,
                    expiration: null,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Cache write failed - still return result to caller
                if (_logger != null)
                    LoggerMessages.CacheSetFailed(_logger, cacheKey, ex);
            }
        }

        // Log successful completion
        if (_logger != null)
        {
            stopwatch?.Stop();
            if (deliveryResult.HasStaleContent)
                LoggerMessages.QueryStaleContent(_logger, "list");
            LoggerMessages.QueryCompleted(_logger, "Items", "list",
                stopwatch?.ElapsedMilliseconds ?? 0, deliveryResult.StatusCode, cacheHit: false);
        }

        return result;
    }

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemListingResponse<TModel>>>>? CreateNextPageFetcher(IPagination pagination)
    {
        if (string.IsNullOrEmpty(pagination.NextPageUrl))
            return null;

        // Calculate next skip value
        var nextSkip = pagination.Skip + pagination.Count;

        return async (ct) =>
        {
            // Create a new query with updated skip
            var nextQuery = new ItemsQuery<TModel>(_api, _getDefaultWaitForNewContent, _contentItemMapper, _cacheManager, _logger)
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

    public async Task<IDeliveryResult<IDeliveryItemListingResponse<TModel>>> ExecuteAllAsync(CancellationToken cancellationToken = default)
    {
        // Log pagination start
        if (_logger != null)
            LoggerMessages.PaginationStarted(_logger, "Items");

        var all = new List<ContentItem<TModel>>();
        var skip = _params.Skip ?? 0;
        var limit = _params.Limit;
        string? requestUrl = null;
        System.Net.Http.Headers.HttpResponseHeaders? responseHeaders = null;
        int pageNumber = 0;

        while (true)
        {
            var pageParams = _params with { Skip = skip, Limit = limit };

            var wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
            var response = await _api.GetItemsInternalAsync<TModel>(
                pageParams,
                FilterQueryParams.ToQueryDictionary(_serializedFilters),
                wait).ConfigureAwait(false);

            // Convert to delivery result
            var deliveryResult = await response.ToDeliveryResultAsync().ConfigureAwait(false);

            if (!deliveryResult.IsSuccess)
            {
                if (_logger != null)
                    LoggerMessages.PaginationStoppedEarly(_logger, "Items");

                return DeliveryResult.Failure<IDeliveryItemListingResponse<TModel>>(
                    deliveryResult.RequestUrl ?? string.Empty,
                    deliveryResult.StatusCode,
                    deliveryResult.Error!,
                    deliveryResult.ResponseHeaders);
            }

            pageNumber++;

            // Capture request URL and headers from first request
            requestUrl ??= deliveryResult.RequestUrl;
            responseHeaders ??= deliveryResult.ResponseHeaders;

            var items = deliveryResult.Value.Items;
            var pageCount = items?.Count ?? 0;

            if (pageCount == 0)
                break;

            if (items is { Count: > 0 })
            {
                foreach (var item in items)
                {
                    // NOTE: ExecuteAllAsync intentionally does NOT use caching - it's for bulk operations where freshness is critical
                    if (!IsDynamicModel)
                    {
                        await _contentItemMapper.CompleteItemAsync(item, deliveryResult.Value.ModularContent, null, cancellationToken).ConfigureAwait(false);
                    }
                    all.Add(item);
                }
            }

            // Log pagination progress
            if (_logger != null)
                LoggerMessages.ItemsPaginationProgress(_logger, pageNumber, all.Count);

            skip += pageCount;

            // Stop if we got fewer than requested (page exhausted)
            if (limit.HasValue && pageCount < limit.Value)
                break;
        }

        // Log pagination completed
        if (_logger != null)
            LoggerMessages.PaginationCompleted(_logger, "Items", pageNumber, all.Count);

        // Create a synthetic response with all items (no next page since we fetched everything)
        var allItemsResponse = new DeliveryItemListingResponse<TModel>
        {
            Items = all,
            Pagination = new Pagination
            {
                Skip = _params.Skip ?? 0,
                Limit = all.Count,
                Count = all.Count,
                NextPageUrl = string.Empty,
                TotalCount = all.Count
            },
            ModularContent = new Dictionary<string, System.Text.Json.JsonElement>(),
            NextPageFetcher = null // No next page
        };

        return DeliveryResult.Success<IDeliveryItemListingResponse<TModel>>(
            allItemsResponse,
            requestUrl ?? string.Empty,
            HttpStatusCode.OK,
            hasStaleContent: false,
            continuationToken: null,
            responseHeaders: responseHeaders);
    }
}