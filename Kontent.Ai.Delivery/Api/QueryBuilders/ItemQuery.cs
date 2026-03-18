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
    Uri? customAssetDomain = null,
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
        _params = _params with { Elements = string.Join(",", elementCodenames) };
        return this;
    }

    public IItemQuery<TModel> WithoutElements(params string[] elementCodenames)
    {
        _params = _params with { ExcludeElements = string.Join(",", elementCodenames) };
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
        IDeliveryResult<DeliveryItemResponse<TModel>>? apiResult = null;
        var factoryInvoked = false;

        var cached = cacheManager.StorageMode == CacheStorageMode.RawJson
            ? await ExecuteWithRawJsonCacheAsync(cacheManager, cacheKey, waitForLoadingNewContent, r => apiResult = r, () => factoryInvoked = true, cancellationToken).ConfigureAwait(false)
            : await ExecuteWithHydratedCacheAsync(cacheManager, cacheKey, waitForLoadingNewContent, r => apiResult = r, () => factoryInvoked = true, cancellationToken).ConfigureAwait(false);

        // Cache hit (factory never called) or fail-safe served stale data after HTTP error
        if (cached is not null && (apiResult is null || !apiResult.IsSuccess))
        {
            LogQueryCompleted(stopwatch, HttpStatusCode.OK, cacheHit: true);
            var isFailSafe = factoryInvoked
                || (cacheManager is IFailSafeStateProvider failSafeProvider && failSafeProvider.IsFailSafeActive(cacheKey));

            return isFailSafe
                ? DeliveryResult.FailSafeHit(cached.Value, cached.DependencyKeys)
                : DeliveryResult.CacheHit(cached.Value, cached.DependencyKeys);
        }

        apiResult = QueryExecutionResultHelper.EnsureApiResult(apiResult, "Item", codename);
        if (!apiResult.IsSuccess)
        {
            LogQueryFailed(apiResult);
            LogQueryCompleted(stopwatch, apiResult.StatusCode, cacheHit: false, apiResult.HasStaleContent);
            return CreateFailureResult(apiResult);
        }

        LogQueryCompleted(stopwatch, apiResult.StatusCode, cacheHit: false, apiResult.HasStaleContent);
        return WrapSuccess(cached?.Value ?? apiResult.Value.Item, apiResult, cached?.DependencyKeys);
    }

    private async Task<CacheResult<IContentItem<TModel>>?> ExecuteWithRawJsonCacheAsync(
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        bool? waitForLoadingNewContent,
        Action<IDeliveryResult<DeliveryItemResponse<TModel>>> captureApiResult,
        Action captureFactoryInvoked,
        CancellationToken cancellationToken)
    {
        var cached = await cacheManager.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                captureFactoryInvoked();
                var result = await FetchFromApiAsync(waitForLoadingNewContent, ct).ConfigureAwait(false);
                captureApiResult(result);
                if (!result.IsSuccess)
                    return null;

                var (item, deps) = await ProcessItemAsync(result.Value, ct).ConfigureAwait(false);
                var rawPayload = CachedRawItemsPayload.FromItem(item, result.Value.ModularContent);
                return new CacheEntry<CachedRawItemsPayload>(rawPayload, deps);
            },
            CacheExpiration,
            cancellationToken).ConfigureAwait(false);

        if (cached is null)
            return null;

        var item = await CachePayloadHelper.RehydrateItemAsync<TModel>(
            cached.Value,
            contentDeserializer,
            contentItemMapper,
            IsDynamicModel,
            defaultRenditionPreset,
            customAssetDomain,
            logger,
            cancellationToken).ConfigureAwait(false);

        return new CacheResult<IContentItem<TModel>>(item, cached.DependencyKeys);
    }

    private async Task<CacheResult<IContentItem<TModel>>?> ExecuteWithHydratedCacheAsync(
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        bool? waitForLoadingNewContent,
        Action<IDeliveryResult<DeliveryItemResponse<TModel>>> captureApiResult,
        Action captureFactoryInvoked,
        CancellationToken cancellationToken)
    {
        return await cacheManager.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                captureFactoryInvoked();
                var result = await FetchFromApiAsync(waitForLoadingNewContent, ct).ConfigureAwait(false);
                captureApiResult(result);
                if (!result.IsSuccess)
                    return null;

                var (item, deps) = await ProcessItemAsync(result.Value, ct).ConfigureAwait(false);
                return new CacheEntry<IContentItem<TModel>>(item, deps);
            },
            CacheExpiration,
            cancellationToken).ConfigureAwait(false);
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

        var (item, dependencyKeys) = await ProcessItemAsync(deliveryResult.Value, cancellationToken).ConfigureAwait(false);
        LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false, deliveryResult.HasStaleContent);
        return WrapSuccess(item, deliveryResult, dependencyKeys);
    }

    private static IDeliveryResult<IContentItem<TModel>> WrapSuccess(
        IContentItem<TModel> item,
        IDeliveryResult<DeliveryItemResponse<TModel>> apiResult,
        IReadOnlyList<string>? dependencyKeys) =>
        DeliveryResult.SuccessFrom(item, apiResult, dependencyKeys);

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

    private async Task<(IContentItem<TModel> Item, string[] Dependencies)> ProcessItemAsync(
        DeliveryItemResponse<TModel> resp, CancellationToken cancellationToken)
    {
        LatestModularContent = resp.ModularContent;
        var item = resp.Item;
        var dependencyContext = new DependencyTrackingContext();

        dependencyContext.TrackItem(item.System.Codename);
        if (resp.ModularContent is not null)
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
                    customAssetDomain,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return (item, [.. dependencyContext.Dependencies]);
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
