using System.Net;
using System.Runtime.CompilerServices;
using Kontent.Ai.Delivery.Api.Filtering;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Kontent.Ai.Delivery.Extensions;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IEnumerateItemsQuery{TModel}"/>
internal sealed class EnumerateItemsQuery<TModel>(
    IDeliveryApi api,
    Func<bool?> getDefaultWaitForNewContent,
    ContentItemMapper contentItemMapper,
    ILogger? logger = null) : IEnumerateItemsQuery<TModel>
{
    private readonly IDeliveryApi _api = api;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly ContentItemMapper _contentItemMapper = contentItemMapper;
    private readonly ILogger? _logger = logger;
    private EnumItemsParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private readonly List<KeyValuePair<string, string>> _serializedFilters = [];
    private string? _continuationToken;
    private static bool IsDynamicModel =>
        typeof(TModel) == typeof(IDynamicElements) ||
        typeof(TModel) == typeof(ContentItems.DynamicElements);

    public IEnumerateItemsQuery<TModel> WithLanguage(string languageCodename, LanguageFallbackMode languageFallbackMode = LanguageFallbackMode.Enabled)
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

    public IEnumerateItemsQuery<TModel> WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public IEnumerateItemsQuery<TModel> OrderBy(string elementOrAttributePath, OrderingMode orderingMode = OrderingMode.Ascending)
    {
        _params = _params with
        {
            OrderBy = orderingMode == OrderingMode.Ascending
                ? $"{elementOrAttributePath}[asc]"
                : $"{elementOrAttributePath}[desc]"
        };
        return this;
    }

    public IEnumerateItemsQuery<TModel> WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public IEnumerateItemsQuery<TModel> Where(Func<IItemsFilterBuilder, IItemsFilterBuilder> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        build(new ItemsFilterBuilder(_serializedFilters));
        return this;
    }

    public async Task<IDeliveryResult<IDeliveryItemsFeedResponse<TModel>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();

        var resp = await _api
            .GetItemsFeedInternalAsync<TModel>(
                _params,
                FilterQueryParams.ToQueryDictionary(_serializedFilters),
                _continuationToken,
                wait,
                cancellationToken)
            .ConfigureAwait(false);

        var deliveryResult = await resp.ToDeliveryResultAsync().ConfigureAwait(false);

        if (!deliveryResult.IsSuccess)
        {
            return DeliveryResult.Failure<IDeliveryItemsFeedResponse<TModel>>(
                deliveryResult.RequestUrl ?? string.Empty,
                deliveryResult.StatusCode,
                deliveryResult.Error,
                deliveryResult.ResponseHeaders);
        }

        var content = deliveryResult.Value;
        var continuationToken = resp.Continuation();

        // Post-process items (hydration for non-dynamic models)
        if (!IsDynamicModel)
        {
            foreach (var item in content.Items)
            {
                await _contentItemMapper.CompleteItemAsync(item, content.ModularContent, null, cancellationToken).ConfigureAwait(false);
            }
        }

        // Build response with next page fetcher
        var responseWithFetcher = content with
        {
            ContinuationToken = continuationToken,
            NextPageFetcher = CreateNextPageFetcher(continuationToken)
        };

        return DeliveryResult.Success<IDeliveryItemsFeedResponse<TModel>>(
            responseWithFetcher,
            deliveryResult.RequestUrl ?? string.Empty,
            deliveryResult.StatusCode,
            deliveryResult.HasStaleContent,
            deliveryResult.ContinuationToken,
            deliveryResult.ResponseHeaders);
    }

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemsFeedResponse<TModel>>>>? CreateNextPageFetcher(string? continuationToken)
    {
        if (string.IsNullOrEmpty(continuationToken))
            return null;

        return async (ct) =>
        {
            var nextQuery = new EnumerateItemsQuery<TModel>(_api, _getDefaultWaitForNewContent, _contentItemMapper, _logger)
            {
                _params = _params,
                _waitForLoadingNewContentOverride = _waitForLoadingNewContentOverride,
                _continuationToken = continuationToken
            };

            // Copy filters
            foreach (var filter in _serializedFilters)
            {
                nextQuery._serializedFilters.Add(filter);
            }

            return await nextQuery.ExecuteAsync(ct).ConfigureAwait(false);
        };
    }

    public async IAsyncEnumerable<IContentItem<TModel>> EnumerateItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Log pagination start
        if (_logger != null)
            LoggerMessages.PaginationStarted(_logger, "ItemsFeed");

        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        string? token = null;
        int pageCount = 0;
        int totalItems = 0;

        while (true)
        {
            var resp = await _api
                .GetItemsFeedInternalAsync<TModel>(
                    _params,
                    FilterQueryParams.ToQueryDictionary(_serializedFilters),
                    token,
                    wait,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode || resp.Content is null)
            {
                if (_logger != null)
                    LoggerMessages.PaginationStoppedEarly(_logger, "ItemsFeed");
                yield break;
            }

            pageCount++;

            foreach (var item in resp.Content.Items)
            {
                // Dynamic mode intentionally stays raw (no hydration).
                if (!IsDynamicModel)
                {
                    await _contentItemMapper.CompleteItemAsync(item, resp.Content.ModularContent, null, cancellationToken).ConfigureAwait(false);
                }
                totalItems++;
                yield return item;
            }

            token = resp.Continuation();
            if (string.IsNullOrEmpty(token))
            {
                if (_logger != null)
                    LoggerMessages.PaginationCompleted(_logger, "ItemsFeed", pageCount, totalItems);
                yield break;
            }
        }
    }

    public async Task<IDeliveryResult<IDeliveryItemsFeedResponse<TModel>>> EnumerateAllAsync(CancellationToken cancellationToken = default)
    {
        // Log pagination start
        if (_logger != null)
            LoggerMessages.PaginationStarted(_logger, "ItemsFeed");

        var allItems = new List<ContentItem<TModel>>();
        string? requestUrl = null;
        System.Net.Http.Headers.HttpResponseHeaders? responseHeaders = null;
        int pageCount = 0;

        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        string? token = null;

        while (true)
        {
            var resp = await _api
                .GetItemsFeedInternalAsync<TModel>(
                    _params,
                    FilterQueryParams.ToQueryDictionary(_serializedFilters),
                    token,
                    wait,
                    cancellationToken)
                .ConfigureAwait(false);

            var deliveryResult = await resp.ToDeliveryResultAsync().ConfigureAwait(false);

            if (!deliveryResult.IsSuccess)
            {
                if (_logger != null)
                    LoggerMessages.PaginationStoppedEarly(_logger, "ItemsFeed");

                return DeliveryResult.Failure<IDeliveryItemsFeedResponse<TModel>>(
                    deliveryResult.RequestUrl ?? string.Empty,
                    deliveryResult.StatusCode,
                    deliveryResult.Error,
                    deliveryResult.ResponseHeaders);
            }

            pageCount++;

            // Capture request URL and headers from first request
            requestUrl ??= deliveryResult.RequestUrl;
            responseHeaders ??= deliveryResult.ResponseHeaders;

            var content = deliveryResult.Value;

            foreach (var item in content.Items)
            {
                // Post-process items (hydration for non-dynamic models)
                if (!IsDynamicModel)
                {
                    await _contentItemMapper.CompleteItemAsync(item, content.ModularContent, null, cancellationToken).ConfigureAwait(false);
                }
                allItems.Add(item);
            }

            token = resp.Continuation();
            if (string.IsNullOrEmpty(token))
                break;
        }

        // Log pagination completed
        if (_logger != null)
            LoggerMessages.PaginationCompleted(_logger, "ItemsFeed", pageCount, allItems.Count);

        // Create synthetic response with all items (no next page since we fetched everything)
        var allItemsResponse = new DeliveryItemsFeedResponse<TModel>
        {
            Items = allItems,
            ModularContent = new Dictionary<string, System.Text.Json.JsonElement>(),
            ContinuationToken = null, // No more pages
            NextPageFetcher = null // No next page
        };

        return DeliveryResult.Success<IDeliveryItemsFeedResponse<TModel>>(
            allItemsResponse,
            requestUrl ?? string.Empty,
            HttpStatusCode.OK,
            hasStaleContent: false,
            continuationToken: null,
            responseHeaders: responseHeaders);
    }
}