using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Kontent.Ai.Delivery.Api.Filtering;
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
    IContentDeserializer contentDeserializer,
    IDeliveryCacheManager? cacheManager,
    ILogger? logger = null) : IItemQuery<TModel>
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly SerializedFilterCollection _serializedFilters = [];
    private SingleItemParams _params = new();
    private readonly ContentItemMapper _contentItemMapper = contentItemMapper;
    private readonly IContentDeserializer _contentDeserializer = contentDeserializer;
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;
    private readonly ILogger? _logger = logger;
    private bool? _waitForLoadingNewContentOverride;
    private static bool IsDynamicModel => ModelTypeHelper.IsDynamic<TModel>();
    internal IReadOnlyDictionary<string, JsonElement>? LatestModularContent { get; private set; }

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
        LatestModularContent = null;
        LogQueryStarting();
        var stopwatch = StartTimingIfEnabled();

        return _cacheManager is not null
            ? await ExecuteWithCacheAsync(_cacheManager, stopwatch, cancellationToken).ConfigureAwait(false)
            : await ExecuteWithoutCacheAsync(stopwatch, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IDeliveryResult<IContentItem<TModel>>> ExecuteWithCacheAsync(
        IDeliveryCacheManager cacheManager,
        Stopwatch? stopwatch,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildItemKey(_codename, _params);
        var (cacheResult, apiResult) = await FetchWithCacheAsync(cacheManager, cacheKey, cancellationToken).ConfigureAwait(false);

        if (cacheResult.IsCacheHit)
        {
            LogQueryCompleted(stopwatch, HttpStatusCode.OK, cacheHit: true);
            return DeliveryResult.CacheHit(cacheResult.Value!);
        }

        if (apiResult is not { IsSuccess: true })
        {
            if (apiResult is not null)
            {
                LogQueryFailed(apiResult);
                return CreateFailureResult(apiResult);
            }

            throw new InvalidOperationException("API result was not captured during fetch.");
        }

        LogQueryCompleted(stopwatch, apiResult.StatusCode, cacheHit: false, apiResult.HasStaleContent);
        return WrapSuccess(cacheResult.Value!, apiResult);
    }

    private async Task<IDeliveryResult<IContentItem<TModel>>> ExecuteWithoutCacheAsync(
        Stopwatch? stopwatch,
        CancellationToken cancellationToken)
    {
        var deliveryResult = await FetchFromApiAsync(cancellationToken).ConfigureAwait(false);
        if (!deliveryResult.IsSuccess)
        {
            LogQueryFailed(deliveryResult);
            return CreateFailureResult(deliveryResult);
        }

        var (item, _) = await ProcessItemAsync(deliveryResult.Value, cancellationToken).ConfigureAwait(false);
        LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false, deliveryResult.HasStaleContent);
        return WrapSuccess(item, deliveryResult);
    }

    private async Task<(CacheFetchResult<IContentItem<TModel>> CacheResult, IDeliveryResult<DeliveryItemResponse<TModel>>? ApiResult)>
        FetchWithCacheAsync(IDeliveryCacheManager cacheManager, string cacheKey, CancellationToken cancellationToken)
    {
        return cacheManager.StorageMode == CacheStorageMode.RawJson
            ? await FetchWithRawJsonCacheAsync(cacheManager, cacheKey, cancellationToken).ConfigureAwait(false)
            : await FetchWithHydratedObjectCacheAsync(cacheManager, cacheKey, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(CacheFetchResult<IContentItem<TModel>> CacheResult, IDeliveryResult<DeliveryItemResponse<TModel>>? ApiResult)>
        FetchWithRawJsonCacheAsync(IDeliveryCacheManager cacheManager, string cacheKey, CancellationToken cancellationToken)
    {
        IDeliveryResult<DeliveryItemResponse<TModel>>? apiResult = null;

        // Distributed cache: cache raw JSON strings to avoid serialization issues
        var cacheResult = await QueryCacheHelper.GetOrFetchWithRehydrationAsync(
            cacheManager,
            cacheKey,
            async ct =>
            {
                apiResult = await FetchFromApiAsync(ct).ConfigureAwait(false);
                if (!apiResult.IsSuccess)
                    return (null, null, Array.Empty<string>());

                var (item, deps) = await ProcessItemAsync(apiResult.Value, ct).ConfigureAwait(false);
                var payload = CachedRawItemsPayload.FromItem(item, apiResult.Value.ModularContent);
                return (payload, item, deps);
            },
            async (payload, ct) => (IContentItem<TModel>)await CachePayloadHelper.RehydrateItemAsync<TModel>(
                payload, _contentDeserializer, _contentItemMapper, IsDynamicModel, ct).ConfigureAwait(false),
            _logger,
            cancellationToken).ConfigureAwait(false);

        return (cacheResult, apiResult);
    }

    private async Task<(CacheFetchResult<IContentItem<TModel>> CacheResult, IDeliveryResult<DeliveryItemResponse<TModel>>? ApiResult)>
        FetchWithHydratedObjectCacheAsync(IDeliveryCacheManager cacheManager, string cacheKey, CancellationToken cancellationToken)
    {
        IDeliveryResult<DeliveryItemResponse<TModel>>? apiResult = null;

        // Memory cache: store hydrated objects directly (no serialization needed)
        var cacheResult = await QueryCacheHelper.GetOrFetchAsync(
            cacheManager,
            cacheKey,
            async ct =>
            {
                apiResult = await FetchFromApiAsync(ct).ConfigureAwait(false);
                if (!apiResult.IsSuccess)
                    return (null, Array.Empty<string>());

                return await ProcessItemAsync(apiResult.Value, ct).ConfigureAwait(false);
            },
            _logger,
            cancellationToken).ConfigureAwait(false);

        return (cacheResult, apiResult);
    }

    private static IDeliveryResult<IContentItem<TModel>> WrapSuccess(
        IContentItem<TModel> item, IDeliveryResult<DeliveryItemResponse<TModel>> apiResult) =>
        DeliveryResult.Success(
            item,
            apiResult.RequestUrl ?? string.Empty,
            apiResult.StatusCode,
            apiResult.HasStaleContent,
            apiResult.ContinuationToken,
            apiResult.ResponseHeaders);

    private async Task<IDeliveryResult<DeliveryItemResponse<TModel>>> FetchFromApiAsync(CancellationToken cancellationToken = default)
    {
        var wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var rawResponse = await _api.GetItemInternalAsync<TModel>(
                _codename,
                _params,
                _serializedFilters.ToQueryDictionary(),
                wait,
                cancellationToken)
            .ConfigureAwait(false);
        return await rawResponse.ToDeliveryResultAsync(_logger).ConfigureAwait(false);
    }

    private async Task<(IContentItem<TModel> Item, IEnumerable<string> Dependencies)> ProcessItemAsync(
        DeliveryItemResponse<TModel> resp, CancellationToken cancellationToken)
    {
        LatestModularContent = resp.ModularContent;
        var item = resp.Item;
        var dependencyContext = _cacheManager is not null ? new DependencyTrackingContext() : null;

        dependencyContext?.TrackItem(item.System.Codename);
        if (dependencyContext is not null && resp.ModularContent is not null)
        {
            foreach (var itemCodename in resp.ModularContent.Keys)
                dependencyContext.TrackItem(itemCodename);
        }

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
        if (_logger is not null)
            LoggerMessages.QueryStarting(_logger, "Item", _codename);
    }

    private Stopwatch? StartTimingIfEnabled() =>
        _logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

    private void LogQueryFailed(IDeliveryResult<DeliveryItemResponse<TModel>> deliveryResult)
    {
        if (_logger is not null)
        {
            LoggerMessages.QueryFailed(_logger, "Item", _codename, deliveryResult.StatusCode,
                deliveryResult.Error?.Message, exception: null);
        }
    }

    private void LogQueryCompleted(Stopwatch? stopwatch, HttpStatusCode statusCode, bool cacheHit, bool hasStaleContent = false)
    {
        if (_logger is null)
            return;
        stopwatch?.Stop();
        if (hasStaleContent)
            LoggerMessages.QueryStaleContent(_logger, _codename);
        LoggerMessages.QueryCompleted(_logger, "Item", _codename,
            stopwatch?.ElapsedMilliseconds ?? 0, statusCode, cacheHit);
    }

}
