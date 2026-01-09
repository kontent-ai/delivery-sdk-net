using System.Diagnostics;
using System.Net;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IItemQuery{TModel}"/>
internal sealed class ItemQuery<TModel>(
    IDeliveryApi api,
    string codename,
    Func<bool?> getDefaultWaitForNewContent,
    ContentItemMapper contentItemMapper,
    IDeliveryCacheManager? cacheManager,
    ILogger? logger = null) : IItemQuery<TModel>
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private SingleItemParams _params = new();
    private readonly ContentItemMapper _contentItemMapper = contentItemMapper;
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;
    private readonly ILogger? _logger = logger;
    private bool? _waitForLoadingNewContentOverride;
    private static bool IsDynamicModel => typeof(TModel) == typeof(IDynamicElements) || typeof(TModel) == typeof(DynamicElements);

    public IItemQuery<TModel> WithLanguage(string languageCodename)
    {
        _params = _params with { Language = languageCodename };
        return this;
    }

    public IItemQuery<TModel> WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public IItemQuery<TModel> WithoutElements(params string[] elementCodenames)
    {
        _params = _params with { ExcludeElements = elementCodenames };
        return this;
    }

    public IItemQuery<TModel> Depth(int depth)
    {
        _params = _params with { Depth = depth };
        return this;
    }

    public IItemQuery<TModel> WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IContentItem<TModel>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Start timing if logging is enabled
        var stopwatch = _logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

        // ========== 1. CACHE CHECK (if enabled) ==========
        string? cacheKey = null;
        if (_cacheManager != null)
        {
            try
            {
                cacheKey = CacheKeyBuilder.BuildItemKey(_codename, _params);
                // Cache ContentItem directly - it's the concrete type that serializes properly
                var cachedItem = await _cacheManager.GetAsync<ContentItem<TModel>>(cacheKey, cancellationToken)
                    .ConfigureAwait(false);

                if (cachedItem != null)
                {
                    // Build DeliveryResult from cached item
                    var cachedResult = DeliveryResult.CacheHit<IContentItem<TModel>>(cachedItem);

                    // Log cache hit and return
                    if (_logger != null)
                    {
                        LoggerMessages.QueryCacheHit(_logger, cacheKey);
                        LoggerMessages.QueryCompleted(_logger, "Item", _codename,
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
        var rawResponse = await _api.GetItemInternalAsync<TModel>(_codename, _params, wait).ConfigureAwait(false);

        // Convert IApiResponse to IDeliveryResult
        var deliveryResult = await rawResponse.ToDeliveryResultAsync().ConfigureAwait(false);

        if (!deliveryResult.IsSuccess)
        {
            // Log query failure
            if (_logger != null)
                LoggerMessages.QueryFailed(_logger, "Item", _codename, deliveryResult.StatusCode,
                    deliveryResult.Error?.Message, exception: null);

            return DeliveryResult.Failure<IContentItem<TModel>>(
                deliveryResult.RequestUrl ?? string.Empty,
                deliveryResult.StatusCode,
                deliveryResult.Error,
                deliveryResult.ResponseHeaders);
        }

        // ========== 3. POST-PROCESS WITH DEPENDENCY TRACKING ==========
        var resp = deliveryResult.Value;
        var item = resp.Item;

        // Create dependency context only if caching enabled
        var dependencyContext = _cacheManager != null ? new DependencyTrackingContext() : null;

        // Track primary item dependency
        dependencyContext?.TrackItem(item.System.Codename);

        // Track modular content dependencies
        if (dependencyContext != null && resp.ModularContent != null)
        {
            foreach (var codename in resp.ModularContent.Keys)
            {
                dependencyContext.TrackItem(codename);
            }
        }

        // Post-process to hydrate IRichTextContent, assets, taxonomy (will track additional dependencies)
        // Dynamic mode intentionally stays raw (no hydration).
        if (!IsDynamicModel)
        {
            await _contentItemMapper.CompleteItemAsync(item, resp.ModularContent, dependencyContext, cancellationToken)
                .ConfigureAwait(false);
        }

        // ========== 4. BUILD RESULT ==========
        var result = DeliveryResult.Success<IContentItem<TModel>>(
            item,
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
                // Cache the ContentItem directly (concrete type for proper serialization)
                if (item is ContentItem<TModel> concreteItem)
                {
                    await _cacheManager.SetAsync(
                        cacheKey,
                        concreteItem,
                        dependencyContext.Dependencies,
                        expiration: null, // Use cache manager's default
                        cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
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
                LoggerMessages.QueryStaleContent(_logger, _codename);
            LoggerMessages.QueryCompleted(_logger, "Item", _codename,
                stopwatch?.ElapsedMilliseconds ?? 0, deliveryResult.StatusCode, cacheHit: false);
        }

        return result;
    }
}