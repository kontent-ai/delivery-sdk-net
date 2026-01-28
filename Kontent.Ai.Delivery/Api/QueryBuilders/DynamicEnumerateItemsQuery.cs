using System.Runtime.CompilerServices;
using Kontent.Ai.Delivery.Api.Filtering;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IDynamicEnumerateItemsQuery"/>
internal sealed class DynamicEnumerateItemsQuery(
    IDeliveryApi api,
    Func<bool?> getDefaultWaitForNewContent,
    ContentItemMapper contentItemMapper,
    ILogger? logger = null) : IDynamicEnumerateItemsQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly ContentItemMapper _contentItemMapper = contentItemMapper;
    private readonly ILogger? _logger = logger;
    private EnumItemsParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private readonly List<KeyValuePair<string, string>> _serializedFilters = [];
    private string? _continuationToken;

    public IDynamicEnumerateItemsQuery WithLanguage(string languageCodename, LanguageFallbackMode languageFallbackMode = LanguageFallbackMode.Enabled)
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

    public IDynamicEnumerateItemsQuery WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public IDynamicEnumerateItemsQuery OrderBy(string elementOrAttributePath, OrderingMode orderingMode = OrderingMode.Ascending)
    {
        _params = _params with
        {
            OrderBy = orderingMode == OrderingMode.Ascending
                ? $"{elementOrAttributePath}[asc]"
                : $"{elementOrAttributePath}[desc]"
        };
        return this;
    }

    public IDynamicEnumerateItemsQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public IDynamicEnumerateItemsQuery Where(Func<IItemsFilterBuilder, IItemsFilterBuilder> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        build(new ItemsFilterBuilder(_serializedFilters));
        return this;
    }

    public async Task<IDeliveryResult<IDeliveryItemsFeedResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();

        var resp = await _api
            .GetItemsFeedInternalAsync<IDynamicElements>(
                _params,
                FilterQueryParams.ToQueryDictionary(_serializedFilters),
                _continuationToken,
                wait,
                cancellationToken)
            .ConfigureAwait(false);

        var deliveryResult = await resp.ToDeliveryResultAsync().ConfigureAwait(false);

        if (!deliveryResult.IsSuccess)
        {
            return DeliveryResult.Failure<IDeliveryItemsFeedResponse>(
                deliveryResult.RequestUrl ?? string.Empty,
                deliveryResult.StatusCode,
                deliveryResult.Error,
                deliveryResult.ResponseHeaders);
        }

        var content = deliveryResult.Value;
        var continuationToken = resp.Continuation();

        // Runtime type resolution for each item
        var runtimeTypedItems = await _contentItemMapper.RuntimeTypeItemsAsync(
            content.Items,
            content.ModularContent,
            cancellationToken).ConfigureAwait(false);

        // Build response with next page fetcher
        var responseWithFetcher = new DynamicDeliveryItemsFeedResponse
        {
            Items = runtimeTypedItems,
            ModularContent = content.ModularContent,
            ContinuationToken = continuationToken,
            NextPageFetcher = CreateNextPageFetcher(continuationToken)
        };

        return DeliveryResult.Success<IDeliveryItemsFeedResponse>(
            responseWithFetcher,
            deliveryResult.RequestUrl ?? string.Empty,
            deliveryResult.StatusCode,
            deliveryResult.HasStaleContent,
            deliveryResult.ContinuationToken,
            deliveryResult.ResponseHeaders);
    }

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemsFeedResponse>>>? CreateNextPageFetcher(string? continuationToken)
    {
        if (string.IsNullOrEmpty(continuationToken))
            return null;

        return async (ct) =>
        {
            var nextQuery = new DynamicEnumerateItemsQuery(_api, _getDefaultWaitForNewContent, _contentItemMapper, _logger)
            {
                _params = _params,
                _waitForLoadingNewContentOverride = _waitForLoadingNewContentOverride,
                _continuationToken = continuationToken
            };

            nextQuery._serializedFilters.AddRange(_serializedFilters);

            return await nextQuery.ExecuteAsync(ct).ConfigureAwait(false);
        };
    }

    public async IAsyncEnumerable<IContentItem> EnumerateItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Log pagination start
        if (_logger != null)
            LoggerMessages.PaginationStarted(_logger, "ItemsFeed (dynamic)");

        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        string? token = null;
        int pageCount = 0;
        int totalItems = 0;

        while (true)
        {
            var resp = await _api
                .GetItemsFeedInternalAsync<IDynamicElements>(
                    _params,
                    FilterQueryParams.ToQueryDictionary(_serializedFilters),
                    token,
                    wait,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode || resp.Content is null)
            {
                if (_logger != null)
                    LoggerMessages.PaginationStoppedEarly(_logger, "ItemsFeed (dynamic)");
                yield break;
            }

            pageCount++;

            foreach (var dynamicItem in resp.Content.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Attempt runtime type resolution
                if (dynamicItem is IRawContentItem rawContentItem && rawContentItem.RawItemJson.HasValue)
                {
                    var runtimeItem = await _contentItemMapper.TryRuntimeTypeItemAsync(
                        rawContentItem.RawItemJson.Value,
                        resp.Content.ModularContent,
                        dependencyContext: null,
                        cancellationToken).ConfigureAwait(false);

                    if (runtimeItem != null)
                    {
                        totalItems++;
                        yield return runtimeItem;
                        continue;
                    }
                }

                // Fall back to dynamic
                totalItems++;
                yield return dynamicItem;
            }

            token = resp.Continuation();
            if (string.IsNullOrEmpty(token))
            {
                if (_logger != null)
                    LoggerMessages.PaginationCompleted(_logger, "ItemsFeed (dynamic)", pageCount, totalItems);
                yield break;
            }
        }
    }
}
