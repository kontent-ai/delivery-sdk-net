using System.Diagnostics;
using System.Net;
using Kontent.Ai.Delivery.Api.Filtering;
using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IItemsQuery{TModel}"/>
internal sealed class ItemsQuery<TModel>(
    IDeliveryApi api,
    ContentItemMapper contentItemMapper,
    IContentDeserializer contentDeserializer,
    ITypeProvider typeProvider,
    IDeliveryCacheManager? cacheManager,
    string? defaultRenditionPreset = null,
    ILogger? logger = null) : IItemsQuery<TModel>, ICacheExpirationConfigurable
{
    private readonly SerializedFilterCollection _serializedFilters = [];
    private ListItemsParams _params = new();
    private bool _waitForLoadingNewContent;
    private bool _typeFilterApplied;
    public TimeSpan? CacheExpiration { get; set; }
    private static bool IsDynamicModel => ModelTypeHelper.IsDynamic<TModel>();

    public IItemsQuery<TModel> WithLanguage(string languageCodename, LanguageFallbackMode languageFallbackMode = LanguageFallbackMode.Enabled)
    {
        _params = _params with { Language = languageCodename };
        if (languageFallbackMode == LanguageFallbackMode.Disabled)
        {
            SystemFilterHelpers.AddSystemLanguageFilter(_serializedFilters, languageCodename);
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
        _waitForLoadingNewContent = enabled;
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
        ApplyGenericTypeFilter();

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

    private async Task<IDeliveryResult<IDeliveryItemListingResponse<TModel>>> ExecuteWithCacheAsync(
        IDeliveryCacheManager cacheManager,
        Stopwatch? stopwatch,
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(cacheManager.StorageMode);
        IDeliveryResult<DeliveryItemListingResponse<TModel>>? apiResult = null;
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
                ? DeliveryResult.FailSafeHit<IDeliveryItemListingResponse<TModel>>(WithNextPageFetcher(cached))
                : DeliveryResult.CacheHit<IDeliveryItemListingResponse<TModel>>(WithNextPageFetcher(cached));
        }

        apiResult = QueryExecutionResultHelper.EnsureApiResult(apiResult, "Items", "list");
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

    private async Task<DeliveryItemListingResponse<TModel>?> ExecuteWithRawJsonCacheAsync(
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        bool? waitForLoadingNewContent,
        Action<IDeliveryResult<DeliveryItemListingResponse<TModel>>> captureApiResult,
        Action captureFactoryInvoked,
        CancellationToken cancellationToken)
    {
        var payload = await cacheManager.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                captureFactoryInvoked();
                var result = await FetchFromApiAsync(waitForLoadingNewContent, ct).ConfigureAwait(false);
                captureApiResult(result);
                if (!result.IsSuccess)
                    return null;

                var (response, deps) = await ProcessItemsAsync(result.Value, ct).ConfigureAwait(false);
                var rawPayload = CachedRawItemsPayload.FromListing(response);
                return new CacheEntry<CachedRawItemsPayload>(rawPayload, deps);
            },
            CacheExpiration,
            cancellationToken).ConfigureAwait(false);

        if (payload is null)
            return null;

        return await CachePayloadHelper.RehydrateListingAsync<TModel>(
            payload,
            contentDeserializer,
            contentItemMapper,
            IsDynamicModel,
            defaultRenditionPreset,
            logger,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<DeliveryItemListingResponse<TModel>?> ExecuteWithHydratedCacheAsync(
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        bool? waitForLoadingNewContent,
        Action<IDeliveryResult<DeliveryItemListingResponse<TModel>>> captureApiResult,
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

                var (response, deps) = await ProcessItemsAsync(result.Value, ct).ConfigureAwait(false);
                return new CacheEntry<DeliveryItemListingResponse<TModel>>(response, deps);
            },
            CacheExpiration,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<IDeliveryResult<IDeliveryItemListingResponse<TModel>>> ExecuteWithoutCacheAsync(
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

        var (resp, _) = await ProcessItemsAsync(deliveryResult.Value, cancellationToken).ConfigureAwait(false);
        LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false, deliveryResult.HasStaleContent);
        return WrapSuccess(WithNextPageFetcher(resp), deliveryResult);
    }

    private DeliveryItemListingResponse<TModel> WithNextPageFetcher(DeliveryItemListingResponse<TModel> resp)
        => resp with { NextPageFetcher = CreateNextPageFetcher(resp.Pagination) };

    private static IDeliveryResult<IDeliveryItemListingResponse<TModel>> WrapSuccess(
        DeliveryItemListingResponse<TModel> response, IDeliveryResult<DeliveryItemListingResponse<TModel>> apiResult) =>
        DeliveryResult.SuccessFrom<IDeliveryItemListingResponse<TModel>, DeliveryItemListingResponse<TModel>>(response, apiResult);

    private void ApplyGenericTypeFilter()
    {
        if (_typeFilterApplied)
            return;
        _typeFilterApplied = true;

        SystemFilterHelpers.AddGenericTypeFilter<TModel>(_serializedFilters, typeProvider, logger);
    }

    private async Task<IDeliveryResult<DeliveryItemListingResponse<TModel>>> FetchFromApiAsync(
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var rawResponse = await api.GetItemsInternalAsync<TModel>(
            _params,
            _serializedFilters.ToQueryDictionary(),
            waitForLoadingNewContent,
            cancellationToken).ConfigureAwait(false);
        return await rawResponse.ToDeliveryResultAsync(logger).ConfigureAwait(false);
    }

    private async Task<(DeliveryItemListingResponse<TModel> Response, IEnumerable<string> Dependencies)> ProcessItemsAsync(
        DeliveryItemListingResponse<TModel> resp, CancellationToken cancellationToken)
    {
        var items = resp.Items;
        var dependencyContext = cacheManager is not null ? new DependencyTrackingContext() : null;

        if (dependencyContext is not null && items is { Count: > 0 })
        {
            foreach (var item in items)
                dependencyContext.TrackItem(item.System.Codename);
        }

        if (dependencyContext is not null && resp.ModularContent is not null)
        {
            foreach (var codename in resp.ModularContent.Keys)
                dependencyContext.TrackItem(codename);
        }

        if (!IsDynamicModel)
        {
            foreach (var item in items)
            {
                await contentItemMapper.CompleteItemAsync(
                        item,
                        resp.ModularContent,
                        dependencyContext,
                        defaultRenditionPreset,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return dependencyContext is null
            ? (resp, [])
            : (resp, [.. dependencyContext.Dependencies, DeliveryCacheDependencies.ItemsListScope]);
    }

    private static IDeliveryResult<IDeliveryItemListingResponse<TModel>> CreateFailureResult(
        IDeliveryResult<DeliveryItemListingResponse<TModel>> deliveryResult) =>
        DeliveryResult.FailureFrom<IDeliveryItemListingResponse<TModel>, DeliveryItemListingResponse<TModel>>(deliveryResult);

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemListingResponse<TModel>>>>? CreateNextPageFetcher(IPagination pagination)
    {
        if (string.IsNullOrEmpty(pagination.NextPageUrl))
            return null;

        var nextSkip = OffsetPaginationHelper.GetNextSkip(pagination);
        var parametersSnapshot = _params;
        var waitForLoadingSnapshot = _waitForLoadingNewContent;
        var typeFilterAppliedSnapshot = _typeFilterApplied;
        var cacheExpirationSnapshot = CacheExpiration;
        var serializedFiltersSnapshot = _serializedFilters.Clone();

        return async (ct) =>
        {
            var nextQuery = new ItemsQuery<TModel>(api, contentItemMapper, contentDeserializer, typeProvider, cacheManager, defaultRenditionPreset, logger)
            {
                _params = parametersSnapshot with { Skip = nextSkip },
                _waitForLoadingNewContent = waitForLoadingSnapshot,
                _typeFilterApplied = typeFilterAppliedSnapshot,
                CacheExpiration = cacheExpirationSnapshot
            };

            nextQuery._serializedFilters.CopyFrom(serializedFiltersSnapshot);

            return await nextQuery.ExecuteAsync(ct).ConfigureAwait(false);
        };
    }

    private string BuildCacheKey(CacheStorageMode storageMode)
    {
        var modelType = storageMode == CacheStorageMode.RawJson ? null : typeof(TModel);
        return CacheKeyBuilder.BuildItemsKey(_params, _serializedFilters, modelType);
    }

    private void LogQueryStarting()
    {
        if (logger is not null)
            LoggerMessages.QueryStarting(logger, "Items", "list");
    }

    private Stopwatch? StartTimingIfEnabled() =>
        logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

    private void LogQueryFailed(IDeliveryResult<DeliveryItemListingResponse<TModel>> deliveryResult)
    {
        if (logger is not null)
        {
            LoggerMessages.QueryFailed(logger, "Items", "list", deliveryResult.StatusCode,
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
        LoggerMessages.QueryCompleted(logger, "Items", "list",
            stopwatch?.ElapsedMilliseconds ?? 0, statusCode, cacheHit);
    }

}
