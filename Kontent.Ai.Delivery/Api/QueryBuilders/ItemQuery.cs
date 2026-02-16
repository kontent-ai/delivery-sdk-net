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
    ContentItemMapper contentItemMapper,
    IContentDeserializer contentDeserializer,
    IDeliveryCacheManager? cacheManager,
    string? defaultRenditionPreset = null,
    ILogger? logger = null) : IItemQuery<TModel>, ICacheExpirationConfigurable
{
    private readonly SerializedFilterCollection _serializedFilters = [];
    private SingleItemParams _params = new();
    private bool _waitForLoadingNewContent;
    public TimeSpan? CacheExpiration { get; set; }
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
        _waitForLoadingNewContent = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IContentItem<TModel>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        LatestModularContent = null;
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

    private async Task<IDeliveryResult<IContentItem<TModel>>> ExecuteWithCacheAsync(
        IDeliveryCacheManager cacheManager,
        Stopwatch? stopwatch,
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(cacheManager.StorageMode);
        var (cacheResult, apiResult) = await FetchWithCacheAsync(
            cacheManager,
            cacheKey,
            waitForLoadingNewContent,
            cancellationToken).ConfigureAwait(false);

        if (QueryExecutionResultHelper.TryGetCacheHitValue(cacheResult, out var cachedItem))
        {
            LogQueryCompleted(stopwatch, HttpStatusCode.OK, cacheHit: true);
            return DeliveryResult.CacheHit(cachedItem);
        }

        apiResult = QueryExecutionResultHelper.EnsureApiResult(apiResult, "Item", codename);
        if (!apiResult.IsSuccess)
        {
            LogQueryFailed(apiResult);
            LogQueryCompleted(stopwatch, apiResult.StatusCode, cacheHit: false, apiResult.HasStaleContent);
            return CreateFailureResult(apiResult);
        }

        LogQueryCompleted(stopwatch, apiResult.StatusCode, cacheHit: false, apiResult.HasStaleContent);
        return WrapSuccess(cacheResult.Value ?? apiResult.Value.Item, apiResult);
    }

    private async Task<IDeliveryResult<IContentItem<TModel>>> ExecuteWithoutCacheAsync(
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

        var (item, _) = await ProcessItemAsync(deliveryResult.Value, cancellationToken).ConfigureAwait(false);
        LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false, deliveryResult.HasStaleContent);
        return WrapSuccess(item, deliveryResult);
    }

    private async Task<(CacheFetchResult<IContentItem<TModel>> CacheResult, IDeliveryResult<DeliveryItemResponse<TModel>>? ApiResult)>
        FetchWithCacheAsync(
            IDeliveryCacheManager cacheManager,
            string cacheKey,
            bool? waitForLoadingNewContent,
            CancellationToken cancellationToken)
    {
        return cacheManager.StorageMode == CacheStorageMode.RawJson
            ? await FetchWithRawJsonCacheAsync(cacheManager, cacheKey, waitForLoadingNewContent, cancellationToken).ConfigureAwait(false)
            : await FetchWithHydratedObjectCacheAsync(cacheManager, cacheKey, waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(CacheFetchResult<IContentItem<TModel>> CacheResult, IDeliveryResult<DeliveryItemResponse<TModel>>? ApiResult)>
        FetchWithRawJsonCacheAsync(
            IDeliveryCacheManager cacheManager,
            string cacheKey,
            bool? waitForLoadingNewContent,
            CancellationToken cancellationToken)
    {
        IDeliveryResult<DeliveryItemResponse<TModel>>? apiResult = null;

        // Distributed cache: cache raw JSON strings to avoid serialization issues
        var cacheResult = await QueryCacheHelper.GetOrFetchWithRehydrationAsync(
            cacheManager,
            cacheKey,
            async ct =>
            {
                apiResult = await FetchFromApiAsync(waitForLoadingNewContent, ct).ConfigureAwait(false);
                if (!apiResult.IsSuccess)
                    return (null, null, Array.Empty<string>());

                var (item, deps) = await ProcessItemAsync(apiResult.Value, ct).ConfigureAwait(false);
                var payload = CachedRawItemsPayload.FromItem(item, apiResult.Value.ModularContent);
                return (payload, item, deps);
            },
            async (payload, ct) => (IContentItem<TModel>)await CachePayloadHelper.RehydrateItemAsync<TModel>(
                payload,
                contentDeserializer,
                contentItemMapper,
                IsDynamicModel,
                defaultRenditionPreset,
                logger,
                ct).ConfigureAwait(false),
            CacheExpiration,
            logger,
            cancellationToken).ConfigureAwait(false);

        return (cacheResult, apiResult);
    }

    private async Task<(CacheFetchResult<IContentItem<TModel>> CacheResult, IDeliveryResult<DeliveryItemResponse<TModel>>? ApiResult)>
        FetchWithHydratedObjectCacheAsync(
            IDeliveryCacheManager cacheManager,
            string cacheKey,
            bool? waitForLoadingNewContent,
            CancellationToken cancellationToken)
    {
        IDeliveryResult<DeliveryItemResponse<TModel>>? apiResult = null;

        // Memory cache: store hydrated objects directly (no serialization needed)
        var cacheResult = await QueryCacheHelper.GetOrFetchAsync(
            cacheManager,
            cacheKey,
            async ct =>
            {
                apiResult = await FetchFromApiAsync(waitForLoadingNewContent, ct).ConfigureAwait(false);
                return !apiResult.IsSuccess ? ((IContentItem<TModel>? Value, IEnumerable<string> Dependencies))(null, Array.Empty<string>()) : ((IContentItem<TModel>? Value, IEnumerable<string> Dependencies))await ProcessItemAsync(apiResult.Value, ct).ConfigureAwait(false);
            },
            CacheExpiration,
            logger,
            cancellationToken).ConfigureAwait(false);

        return (cacheResult, apiResult);
    }

    private static IDeliveryResult<IContentItem<TModel>> WrapSuccess(
        IContentItem<TModel> item, IDeliveryResult<DeliveryItemResponse<TModel>> apiResult) =>
        DeliveryResult.SuccessFrom(item, apiResult);

    private async Task<IDeliveryResult<DeliveryItemResponse<TModel>>> FetchFromApiAsync(
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken = default)
    {
        var rawResponse = await api.GetItemInternalAsync<TModel>(
                codename,
                _params,
                _serializedFilters.ToQueryDictionary(),
                waitForLoadingNewContent,
                cancellationToken)
            .ConfigureAwait(false);
        return await rawResponse.ToDeliveryResultAsync(logger).ConfigureAwait(false);
    }

    private async Task<(IContentItem<TModel> Item, IEnumerable<string> Dependencies)> ProcessItemAsync(
        DeliveryItemResponse<TModel> resp, CancellationToken cancellationToken)
    {
        LatestModularContent = resp.ModularContent;
        var item = resp.Item;
        var dependencyContext = cacheManager is not null ? new DependencyTrackingContext() : null;

        dependencyContext?.TrackItem(item.System.Codename);
        if (dependencyContext is not null && resp.ModularContent is not null)
        {
            foreach (var itemCodename in resp.ModularContent.Keys)
                dependencyContext.TrackItem(itemCodename);
        }

        if (!IsDynamicModel)
        {
            await contentItemMapper.CompleteItemAsync(
                    item,
                    resp.ModularContent,
                    dependencyContext,
                    defaultRenditionPreset,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return (item, dependencyContext?.Dependencies ?? []);
    }

    private static IDeliveryResult<IContentItem<TModel>> CreateFailureResult(
        IDeliveryResult<DeliveryItemResponse<TModel>> deliveryResult) =>
        DeliveryResult.FailureFrom<IContentItem<TModel>, DeliveryItemResponse<TModel>>(deliveryResult);

    private string BuildCacheKey(CacheStorageMode storageMode)
    {
        var modelType = storageMode == CacheStorageMode.RawJson ? null : typeof(TModel);
        return CacheKeyBuilder.BuildItemKey(codename, _params, modelType);
    }

    private void LogQueryStarting()
    {
        if (logger is not null)
            LoggerMessages.QueryStarting(logger, "Item", codename);
    }

    private Stopwatch? StartTimingIfEnabled() =>
        logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

    private void LogQueryFailed(IDeliveryResult<DeliveryItemResponse<TModel>> deliveryResult)
    {
        if (logger is not null)
        {
            LoggerMessages.QueryFailed(logger, "Item", codename, deliveryResult.StatusCode,
                deliveryResult.Error?.Message, exception: null);
        }
    }

    private void LogQueryCompleted(Stopwatch? stopwatch, HttpStatusCode statusCode, bool cacheHit, bool hasStaleContent = false)
    {
        if (logger is null)
            return;
        stopwatch?.Stop();
        if (hasStaleContent)
            LoggerMessages.QueryStaleContent(logger, codename);
        LoggerMessages.QueryCompleted(logger, "Item", codename,
            stopwatch?.ElapsedMilliseconds ?? 0, statusCode, cacheHit);
    }

}
