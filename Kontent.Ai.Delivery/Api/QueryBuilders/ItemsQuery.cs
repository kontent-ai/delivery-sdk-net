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
    ITypeProvider typeProvider,
    IDeliveryCacheManager? cacheManager,
    ILogger? logger = null) : IItemsQuery<TModel>
{
    private readonly IDeliveryApi _api = api;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly ContentItemMapper _contentItemMapper = contentItemMapper;
    private readonly ITypeProvider _typeProvider = typeProvider;
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
        var filterBuilder = new ItemsFilterBuilder(_serializedFilters);
        build(filterBuilder);
        return this;
    }

    public async Task<IDeliveryResult<IDeliveryItemListingResponse<TModel>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Apply generic type filter before execution
        ApplyGenericTypeFilter();

        LogQueryStarting();
        var stopwatch = StartTimingIfEnabled();

        if (_cacheManager != null)
        {
            var cacheKey = CacheKeyBuilder.BuildItemsKey(_params, _serializedFilters);
            IDeliveryResult<DeliveryItemListingResponse<TModel>>? apiResult = null;

            // Use stampede-protected cache fetch
            var cacheResult = await QueryCacheHelper.GetOrFetchAsync(
                _cacheManager,
                cacheKey,
                async ct =>
                {
                    apiResult = await FetchFromApiAsync(ct).ConfigureAwait(false);
                    if (!apiResult.IsSuccess)
                        return (null, Array.Empty<string>());
                    // Post-process items (hydration, dependency tracking)
                    var (response, deps) = await ProcessItemsAsync(apiResult.Value, ct).ConfigureAwait(false);
                    return (response, deps);
                },
                _logger,
                cancellationToken).ConfigureAwait(false);

            if (cacheResult.IsCacheHit)
            {
                // Reconstruct with NextPageFetcher (delegates can't be cached)
                var cachedWithFetcher = cacheResult.Value! with { NextPageFetcher = CreateNextPageFetcher(cacheResult.Value!.Pagination) };
                LogQueryCompleted(stopwatch, HttpStatusCode.OK, cacheHit: true);
                return DeliveryResult.CacheHit<IDeliveryItemListingResponse<TModel>>(cachedWithFetcher);
            }

            // Not a cache hit - check if API succeeded
            if (!apiResult!.IsSuccess)
            {
                LogQueryFailed(apiResult);
                return CreateFailureResult(apiResult);
            }

            // Fresh fetch succeeded - build result with fetcher
            var responseWithFetcher = cacheResult.Value! with { NextPageFetcher = CreateNextPageFetcher(cacheResult.Value!.Pagination) };
            LogQueryCompleted(stopwatch, apiResult.StatusCode, cacheHit: false, apiResult.HasStaleContent);
            return DeliveryResult.Success<IDeliveryItemListingResponse<TModel>>(
                responseWithFetcher,
                apiResult.RequestUrl ?? string.Empty,
                apiResult.StatusCode,
                apiResult.HasStaleContent,
                apiResult.ContinuationToken,
                apiResult.ResponseHeaders);
        }

        // No caching - direct fetch
        var deliveryResult = await FetchFromApiAsync(cancellationToken).ConfigureAwait(false);
        if (!deliveryResult.IsSuccess)
        {
            LogQueryFailed(deliveryResult);
            return CreateFailureResult(deliveryResult);
        }

        var (resp, _) = await ProcessItemsAsync(deliveryResult.Value, cancellationToken).ConfigureAwait(false);
        var respWithFetcher = resp with { NextPageFetcher = CreateNextPageFetcher(resp.Pagination) };
        LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false, deliveryResult.HasStaleContent);
        return DeliveryResult.Success<IDeliveryItemListingResponse<TModel>>(
            respWithFetcher,
            deliveryResult.RequestUrl ?? string.Empty,
            deliveryResult.StatusCode,
            deliveryResult.HasStaleContent,
            deliveryResult.ContinuationToken,
            deliveryResult.ResponseHeaders);
    }

    private void ApplyGenericTypeFilter()
    {
        // Skip for dynamic models
        if (IsDynamicModel)
            return;

        // Get the codename for the generic type
        var codename = _typeProvider.GetCodename(typeof(TModel));

        if (string.IsNullOrEmpty(codename))
        {
            // Type provider doesn't know about this type - don't add filter
            if (_logger != null)
            {
                LoggerMessages.GenericQueryTypeCodenameNotFound(_logger, typeof(TModel).Name);
            }
            return;
        }

        // Add the generic type filter
        _serializedFilters.Add(new KeyValuePair<string, string>(
            FilterPath.System("type") + FilterSuffix.Eq,
            FilterValueSerializer.Serialize(codename)));
    }

    private async Task<IDeliveryResult<DeliveryItemListingResponse<TModel>>> FetchFromApiAsync(CancellationToken cancellationToken)
    {
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var rawResponse = await _api.GetItemsInternalAsync<TModel>(
            _params,
            FilterQueryParams.ToQueryDictionary(_serializedFilters),
            wait,
            cancellationToken).ConfigureAwait(false);
        return await rawResponse.ToDeliveryResultAsync(_logger).ConfigureAwait(false);
    }

    private async Task<(DeliveryItemListingResponse<TModel> Response, IEnumerable<string> Dependencies)> ProcessItemsAsync(
        DeliveryItemListingResponse<TModel> resp, CancellationToken cancellationToken)
    {
        var items = resp.Items;
        var dependencyContext = _cacheManager != null ? new DependencyTrackingContext() : null;

        // Track all items in the response
        if (dependencyContext != null && items is { Count: > 0 })
        {
            foreach (var item in items)
                dependencyContext.TrackItem(item.System.Codename);
        }

        // Track modular content dependencies
        if (dependencyContext != null && resp.ModularContent != null)
        {
            foreach (var codename in resp.ModularContent.Keys)
                dependencyContext.TrackItem(codename);
        }

        // Hydrate each item (tracks additional dependencies: assets, taxonomies)
        // Dynamic mode intentionally stays raw (no hydration)
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
        DeliveryResult.Failure<IDeliveryItemListingResponse<TModel>>(
            deliveryResult.RequestUrl ?? string.Empty,
            deliveryResult.StatusCode,
            deliveryResult.Error,
            deliveryResult.ResponseHeaders);

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemListingResponse<TModel>>>>? CreateNextPageFetcher(IPagination pagination)
    {
        if (string.IsNullOrEmpty(pagination.NextPageUrl))
            return null;

        var nextSkip = ExtractSkipFromUrl(pagination.NextPageUrl);

        return async (ct) =>
        {
            var nextQuery = new ItemsQuery<TModel>(_api, _getDefaultWaitForNewContent, _contentItemMapper, _typeProvider, _cacheManager, _logger)
            {
                _params = _params with { Skip = nextSkip },
                _waitForLoadingNewContentOverride = _waitForLoadingNewContentOverride
            };

            foreach (var filter in _serializedFilters)
                nextQuery._serializedFilters.Add(filter);

            return await nextQuery.ExecuteAsync(ct).ConfigureAwait(false);
        };
    }

    private static int ExtractSkipFromUrl(string url)
    {
        var uri = new Uri(url);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return int.TryParse(query["skip"], out var skip) ? skip : 0;
    }

    private void LogQueryStarting()
    {
        if (_logger != null)
            LoggerMessages.QueryStarting(_logger, "Items", "list");
    }

    private Stopwatch? StartTimingIfEnabled() =>
        _logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

    private void LogQueryFailed(IDeliveryResult<DeliveryItemListingResponse<TModel>> deliveryResult)
    {
        if (_logger != null)
            LoggerMessages.QueryFailed(_logger, "Items", "list", deliveryResult.StatusCode,
                deliveryResult.Error?.Message, exception: null);
    }

    private void LogQueryCompleted(Stopwatch? stopwatch, HttpStatusCode statusCode, bool cacheHit, bool hasStaleContent = false)
    {
        if (_logger == null)
            return;
        stopwatch?.Stop();
        if (hasStaleContent)
            LoggerMessages.QueryStaleContent(_logger, "list");
        LoggerMessages.QueryCompleted(_logger, "Items", "list",
            stopwatch?.ElapsedMilliseconds ?? 0, statusCode, cacheHit);
    }
}

