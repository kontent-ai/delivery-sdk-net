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
    Func<bool?> getDefaultWaitForNewContent,
    ContentItemMapper contentItemMapper,
    IContentDeserializer contentDeserializer,
    ITypeProvider typeProvider,
    IDeliveryCacheManager? cacheManager,
    ILogger? logger = null) : IItemsQuery<TModel>
{
    private readonly IDeliveryApi _api = api;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly ContentItemMapper _contentItemMapper = contentItemMapper;
    private readonly IContentDeserializer _contentDeserializer = contentDeserializer;
    private readonly ITypeProvider _typeProvider = typeProvider;
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;
    private readonly ILogger? _logger = logger;
    private readonly SerializedFilterCollection _serializedFilters = [];
    private ListItemsParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
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
        ApplyGenericTypeFilter();

        LogQueryStarting();
        var stopwatch = StartTimingIfEnabled();

        return _cacheManager is not null
            ? await ExecuteWithCacheAsync(_cacheManager, stopwatch, cancellationToken).ConfigureAwait(false)
            : await ExecuteWithoutCacheAsync(stopwatch, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IDeliveryResult<IDeliveryItemListingResponse<TModel>>> ExecuteWithCacheAsync(
        IDeliveryCacheManager cacheManager,
        Stopwatch? stopwatch,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildItemsKey(_params, _serializedFilters);
        var (cacheResult, apiResult) = await FetchWithCacheAsync(cacheManager, cacheKey, cancellationToken).ConfigureAwait(false);

        if (cacheResult.IsCacheHit)
        {
            LogQueryCompleted(stopwatch, HttpStatusCode.OK, cacheHit: true);
            return DeliveryResult.CacheHit<IDeliveryItemListingResponse<TModel>>(
                WithNextPageFetcher(cacheResult.Value!));
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
        return WrapSuccess(WithNextPageFetcher(cacheResult.Value!), apiResult);
    }

    private async Task<IDeliveryResult<IDeliveryItemListingResponse<TModel>>> ExecuteWithoutCacheAsync(
        Stopwatch? stopwatch,
        CancellationToken cancellationToken)
    {
        var deliveryResult = await FetchFromApiAsync(cancellationToken).ConfigureAwait(false);
        if (!deliveryResult.IsSuccess)
        {
            LogQueryFailed(deliveryResult);
            return CreateFailureResult(deliveryResult);
        }

        var (resp, _) = await ProcessItemsAsync(deliveryResult.Value, cancellationToken).ConfigureAwait(false);
        LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false, deliveryResult.HasStaleContent);
        return WrapSuccess(WithNextPageFetcher(resp), deliveryResult);
    }

    private async Task<(CacheFetchResult<DeliveryItemListingResponse<TModel>> CacheResult, IDeliveryResult<DeliveryItemListingResponse<TModel>>? ApiResult)>
        FetchWithCacheAsync(IDeliveryCacheManager cacheManager, string cacheKey, CancellationToken cancellationToken)
    {
        return cacheManager.StorageMode == CacheStorageMode.RawJson
            ? await FetchWithRawJsonCacheAsync(cacheManager, cacheKey, cancellationToken).ConfigureAwait(false)
            : await FetchWithHydratedObjectCacheAsync(cacheManager, cacheKey, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(CacheFetchResult<DeliveryItemListingResponse<TModel>> CacheResult, IDeliveryResult<DeliveryItemListingResponse<TModel>>? ApiResult)>
        FetchWithRawJsonCacheAsync(IDeliveryCacheManager cacheManager, string cacheKey, CancellationToken cancellationToken)
    {
        IDeliveryResult<DeliveryItemListingResponse<TModel>>? apiResult = null;

        // Distributed cache: cache raw JSON strings to avoid serialization issues
        var cacheResult = await QueryCacheHelper.GetOrFetchWithRehydrationAsync(
            cacheManager,
            cacheKey,
            async ct =>
            {
                apiResult = await FetchFromApiAsync(ct).ConfigureAwait(false);
                if (!apiResult.IsSuccess)
                    return (null, null, Array.Empty<string>());

                var (response, deps) = await ProcessItemsAsync(apiResult.Value, ct).ConfigureAwait(false);
                var payload = CachedRawItemsPayload.FromListing(response);
                return (payload, response, deps);
            },
            (payload, ct) => CachePayloadHelper.RehydrateListingAsync<TModel>(
                payload, _contentDeserializer, _contentItemMapper, IsDynamicModel, ct),
            _logger,
            cancellationToken).ConfigureAwait(false);

        return (cacheResult, apiResult);
    }

    private async Task<(CacheFetchResult<DeliveryItemListingResponse<TModel>> CacheResult, IDeliveryResult<DeliveryItemListingResponse<TModel>>? ApiResult)>
        FetchWithHydratedObjectCacheAsync(IDeliveryCacheManager cacheManager, string cacheKey, CancellationToken cancellationToken)
    {
        IDeliveryResult<DeliveryItemListingResponse<TModel>>? apiResult = null;

        // Memory cache: store hydrated objects directly (no serialization needed)
        var cacheResult = await QueryCacheHelper.GetOrFetchAsync(
            cacheManager,
            cacheKey,
            async ct =>
            {
                apiResult = await FetchFromApiAsync(ct).ConfigureAwait(false);
                if (!apiResult.IsSuccess)
                    return (null, Array.Empty<string>());

                var (response, deps) = await ProcessItemsAsync(apiResult.Value, ct).ConfigureAwait(false);
                return (response, deps);
            },
            _logger,
            cancellationToken).ConfigureAwait(false);

        return (cacheResult, apiResult);
    }

    private DeliveryItemListingResponse<TModel> WithNextPageFetcher(DeliveryItemListingResponse<TModel> resp)
        => resp with { NextPageFetcher = CreateNextPageFetcher(resp.Pagination) };

    private static IDeliveryResult<IDeliveryItemListingResponse<TModel>> WrapSuccess(
        DeliveryItemListingResponse<TModel> response, IDeliveryResult<DeliveryItemListingResponse<TModel>> apiResult) =>
        DeliveryResult.SuccessFrom<IDeliveryItemListingResponse<TModel>, DeliveryItemListingResponse<TModel>>(response, apiResult);

    private void ApplyGenericTypeFilter() => SystemFilterHelpers.AddGenericTypeFilter<TModel>(_serializedFilters, _typeProvider, _logger);

    private async Task<IDeliveryResult<DeliveryItemListingResponse<TModel>>> FetchFromApiAsync(CancellationToken cancellationToken)
    {
        var wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var rawResponse = await _api.GetItemsInternalAsync<TModel>(
            _params,
            _serializedFilters.ToQueryDictionary(),
            wait,
            cancellationToken).ConfigureAwait(false);
        return await rawResponse.ToDeliveryResultAsync(_logger).ConfigureAwait(false);
    }

    private async Task<(DeliveryItemListingResponse<TModel> Response, IEnumerable<string> Dependencies)> ProcessItemsAsync(
        DeliveryItemListingResponse<TModel> resp, CancellationToken cancellationToken)
    {
        var items = resp.Items;
        var dependencyContext = _cacheManager is not null ? new DependencyTrackingContext() : null;

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
                await _contentItemMapper.CompleteItemAsync(item, resp.ModularContent, dependencyContext, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return (resp, dependencyContext?.Dependencies ?? []);
    }

    private static IDeliveryResult<IDeliveryItemListingResponse<TModel>> CreateFailureResult(
        IDeliveryResult<DeliveryItemListingResponse<TModel>> deliveryResult) =>
        DeliveryResult.FailureFrom<IDeliveryItemListingResponse<TModel>, DeliveryItemListingResponse<TModel>>(deliveryResult);

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemListingResponse<TModel>>>>? CreateNextPageFetcher(IPagination pagination)
    {
        if (string.IsNullOrEmpty(pagination.NextPageUrl))
            return null;

        var nextSkip = ExtractSkipFromUrl(pagination.NextPageUrl);

        return async (ct) =>
        {
            var nextQuery = new ItemsQuery<TModel>(_api, _getDefaultWaitForNewContent, _contentItemMapper, _contentDeserializer, _typeProvider, _cacheManager, _logger)
            {
                _params = _params with { Skip = nextSkip },
                _waitForLoadingNewContentOverride = _waitForLoadingNewContentOverride
            };

            nextQuery._serializedFilters.CopyFrom(_serializedFilters);

            return await nextQuery.ExecuteAsync(ct).ConfigureAwait(false);
        };
    }

    private static int ExtractSkipFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return 0;

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return int.TryParse(query["skip"], out var skip) ? skip : 0;
    }

    private void LogQueryStarting()
    {
        if (_logger is not null)
            LoggerMessages.QueryStarting(_logger, "Items", "list");
    }

    private Stopwatch? StartTimingIfEnabled() =>
        _logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

    private void LogQueryFailed(IDeliveryResult<DeliveryItemListingResponse<TModel>> deliveryResult)
    {
        if (_logger is not null)
        {
            LoggerMessages.QueryFailed(_logger, "Items", "list", deliveryResult.StatusCode,
                deliveryResult.Error?.Message, exception: null);
        }
    }

    private void LogQueryCompleted(Stopwatch? stopwatch, HttpStatusCode statusCode, bool cacheHit, bool hasStaleContent = false)
    {
        if (_logger is null)
            return;
        stopwatch?.Stop();
        if (hasStaleContent)
            LoggerMessages.QueryStaleContent(_logger, "list");
        LoggerMessages.QueryCompleted(_logger, "Items", "list",
            stopwatch?.ElapsedMilliseconds ?? 0, statusCode, cacheHit);
    }

}
