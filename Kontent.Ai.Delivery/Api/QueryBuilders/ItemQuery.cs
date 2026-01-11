using System.Diagnostics;
using System.Net;
using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;
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
        LogQueryStarting();
        var stopwatch = StartTimingIfEnabled();

        // 1. CACHE CHECK
        string? cacheKey = null;
        if (_cacheManager != null)
        {
            cacheKey = CacheKeyBuilder.BuildItemKey(_codename, _params);
            var cached = await QueryCacheHelper.TryGetCachedAsync<ContentItem<TModel>>(
                _cacheManager, cacheKey, _logger, cancellationToken).ConfigureAwait(false);
            if (cached != null)
            {
                LogQueryCompleted(stopwatch, HttpStatusCode.OK, cacheHit: true);
                return DeliveryResult.CacheHit<IContentItem<TModel>>(cached);
            }
        }

        // 2. API CALL
        var deliveryResult = await FetchFromApiAsync().ConfigureAwait(false);
        if (!deliveryResult.IsSuccess)
        {
            LogQueryFailed(deliveryResult);
            return CreateFailureResult(deliveryResult);
        }

        // 3. POST-PROCESS
        var (item, dependencies) = await ProcessItemAsync(deliveryResult.Value, cancellationToken)
            .ConfigureAwait(false);

        // 4. BUILD RESULT
        var result = DeliveryResult.Success(
            item,
            deliveryResult.RequestUrl ?? string.Empty,
            deliveryResult.StatusCode,
            deliveryResult.HasStaleContent,
            deliveryResult.ContinuationToken,
            deliveryResult.ResponseHeaders);

        // 5. CACHE & LOG COMPLETION
        if (_cacheManager != null && cacheKey != null && item is ContentItem<TModel> concreteItem)
        {
            await QueryCacheHelper.TrySetCachedAsync(
                _cacheManager, cacheKey, concreteItem, dependencies, _logger, cancellationToken)
                .ConfigureAwait(false);
        }

        LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false, deliveryResult.HasStaleContent);
        return result;
    }

    private async Task<IDeliveryResult<DeliveryItemResponse<TModel>>> FetchFromApiAsync()
    {
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var rawResponse = await _api.GetItemInternalAsync<TModel>(_codename, _params, wait)
            .ConfigureAwait(false);
        return await rawResponse.ToDeliveryResultAsync().ConfigureAwait(false);
    }

    private async Task<(IContentItem<TModel> Item, IEnumerable<string> Dependencies)> ProcessItemAsync(
        DeliveryItemResponse<TModel> resp, CancellationToken cancellationToken)
    {
        var item = resp.Item;
        var dependencyContext = _cacheManager != null ? new DependencyTrackingContext() : null;

        dependencyContext?.TrackItem(item.System.Codename);
        if (dependencyContext != null && resp.ModularContent != null)
        {
            foreach (var codename in resp.ModularContent.Keys)
                dependencyContext.TrackItem(codename);
        }

        // Hydrate rich text, assets, taxonomy (tracks additional dependencies)
        // Dynamic mode intentionally stays raw (no hydration)
        if (!IsDynamicModel)
        {
            await _contentItemMapper.CompleteItemAsync(item, resp.ModularContent, dependencyContext, cancellationToken)
                .ConfigureAwait(false);
        }

        return (item, dependencyContext?.Dependencies ?? []);
    }

    private static IDeliveryResult<IContentItem<TModel>> CreateFailureResult(
        IDeliveryResult<DeliveryItemResponse<TModel>> deliveryResult) =>
        DeliveryResult.Failure<IContentItem<TModel>>(
            deliveryResult.RequestUrl ?? string.Empty,
            deliveryResult.StatusCode,
            deliveryResult.Error,
            deliveryResult.ResponseHeaders);

    private void LogQueryStarting()
    {
        if (_logger != null)
            LoggerMessages.QueryStarting(_logger, "Item", _codename);
    }

    private Stopwatch? StartTimingIfEnabled() =>
        _logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

    private void LogQueryFailed(IDeliveryResult<DeliveryItemResponse<TModel>> deliveryResult)
    {
        if (_logger != null)
            LoggerMessages.QueryFailed(_logger, "Item", _codename, deliveryResult.StatusCode,
                deliveryResult.Error?.Message, exception: null);
    }

    private void LogQueryCompleted(Stopwatch? stopwatch, HttpStatusCode statusCode, bool cacheHit, bool hasStaleContent = false)
    {
        if (_logger == null) return;
        stopwatch?.Stop();
        if (hasStaleContent)
            LoggerMessages.QueryStaleContent(_logger, _codename);
        LoggerMessages.QueryCompleted(_logger, "Item", _codename,
            stopwatch?.ElapsedMilliseconds ?? 0, statusCode, cacheHit);
    }
}