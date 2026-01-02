using System.Diagnostics;
using System.Net;
using Kontent.Ai.Delivery.Abstractions.ContentItems.Processing;
using Kontent.Ai.Delivery.Api.Filtering;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IItemsQuery{TModel}"/>
internal sealed class ItemsQuery<TModel>(
    IDeliveryApi api,
    Func<bool?> getDefaultWaitForNewContent,
    IElementsPostProcessor elementsPostProcessor,
    IDeliveryCacheManager? cacheManager,
    ILogger? logger = null) : IItemsQuery<TModel>
{
    private readonly IDeliveryApi _api = api;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly IElementsPostProcessor _elementsPostProcessor = elementsPostProcessor;
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;
    private readonly ILogger? _logger = logger;
    private readonly Dictionary<string, string> _serializedFilters = [];
    private ListItemsParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private static bool IsDynamicModel => typeof(TModel) == typeof(IDynamicElements) || typeof(TModel) == typeof(DynamicElements);

    public IItemsQuery<TModel> WithLanguage(string languageCodename)
    {
        _params = _params with { Language = languageCodename };
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

    public IItemsQuery<TModel> OrderBy(string elementOrAttributePath, bool ascending = true)
    {
        _params = _params with { OrderBy = ascending ? $"{elementOrAttributePath}[asc]" : $"{elementOrAttributePath}[desc]" };
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

    public IItemsQuery<TModel> Filter(Func<IItemsFilterBuilder, IItemsFilterBuilder> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        build(new ItemsFilterBuilder(_serializedFilters));
        return this;
    }

    public async Task<IDeliveryResult<IReadOnlyList<IContentItem<TModel>>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Start timing if logging is enabled
        var stopwatch = _logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

        // ========== 1. CACHE CHECK (if enabled) ==========
        string? cacheKey = null;
        if (_cacheManager != null)
        {
            try
            {
                cacheKey = CacheKeyBuilder.BuildItemsKey(_params, _serializedFilters);
                // Cache List<ContentItem> directly - concrete type for proper serialization
                var cachedItems = await _cacheManager.GetAsync<List<ContentItem<TModel>>>(cacheKey, cancellationToken)
                    .ConfigureAwait(false);

                if (cachedItems != null)
                {
                    // Build DeliveryResult from cached items
                    var interfaceItems = cachedItems.Cast<IContentItem<TModel>>().ToList().AsReadOnly();
                    var cachedResult = DeliveryResult.CacheHit<IReadOnlyList<IContentItem<TModel>>>(interfaceItems);

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
        var rawResponse = await _api.GetItemsInternalAsync<TModel>(_params, _serializedFilters, wait).ConfigureAwait(false);

        // Convert IApiResponse to IDeliveryResult
        var deliveryResult = await rawResponse.ToDeliveryResultAsync().ConfigureAwait(false);

        if (!deliveryResult.IsSuccess)
        {
            // Log query failure
            if (_logger != null)
                LoggerMessages.QueryFailed(_logger, "Items", "list", deliveryResult.StatusCode,
                    deliveryResult.Error?.Message, exception: null);

            return DeliveryResult.Failure<IReadOnlyList<IContentItem<TModel>>>(
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
                await _elementsPostProcessor.ProcessAsync(item, resp.ModularContent, dependencyContext, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        // ========== 4. BUILD RESULT ==========
        var result = DeliveryResult.Success<IReadOnlyList<IContentItem<TModel>>>(
            items,
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
                // Cache List<ContentItem> directly (concrete type for proper serialization)
                var concreteItems = items
                    .OfType<ContentItem<TModel>>()
                    .ToList();

                // Warn if some items couldn't be cached due to type mismatch
                if (concreteItems.Count != items.Count && _logger != null)
                {
                    LoggerMessages.CachePartialItemsWarning(_logger, concreteItems.Count, items.Count);
                }

                await _cacheManager.SetAsync(
                    cacheKey,
                    concreteItems,
                    dependencyContext.Dependencies,
                    expiration: null, // Use cache manager's default
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

    public async Task<IDeliveryResult<IReadOnlyList<IContentItem<TModel>>>> ExecuteAllAsync(CancellationToken cancellationToken = default)
    {
        var all = new List<IContentItem<TModel>>();
        var skip = _params.Skip ?? 0;
        var limit = _params.Limit;
        string? requestUrl = null;
        System.Net.Http.Headers.HttpResponseHeaders? responseHeaders = null;

        while (true)
        {
            var pageParams = _params with { Skip = skip, Limit = limit };

            var wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
            var response = await _api.GetItemsInternalAsync<TModel>(pageParams, _serializedFilters, wait).ConfigureAwait(false);

            // Convert to delivery result
            var deliveryResult = await response.ToDeliveryResultAsync().ConfigureAwait(false);

            if (!deliveryResult.IsSuccess)
            {
                return DeliveryResult.Failure<IReadOnlyList<IContentItem<TModel>>>(
                    deliveryResult.RequestUrl ?? string.Empty,
                    deliveryResult.StatusCode,
                    deliveryResult.Error!,
                    deliveryResult.ResponseHeaders);
            }

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
                        await _elementsPostProcessor.ProcessAsync(item, deliveryResult.Value.ModularContent, null, cancellationToken).ConfigureAwait(false);
                    }
                    all.Add(item);
                }
            }

            skip += pageCount;

            // Stop if we got fewer than requested (page exhausted)
            if (limit.HasValue && pageCount < limit.Value)
                break;
        }

        return DeliveryResult.Success<IReadOnlyList<IContentItem<TModel>>>(
            all.AsReadOnly(),
            requestUrl ?? string.Empty,
            HttpStatusCode.OK,
            hasStaleContent: false,
            continuationToken: null,
            responseHeaders: responseHeaders);
    }
}