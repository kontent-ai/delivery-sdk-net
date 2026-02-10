using System.Runtime.CompilerServices;
using Kontent.Ai.Delivery.Api.Filtering;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IEnumerateItemsQuery{TModel}"/>
internal sealed class EnumerateItemsQuery<TModel>(
    IDeliveryApi api,
    Func<bool?> getDefaultWaitForNewContent,
    ContentItemMapper contentItemMapper,
    ITypeProvider typeProvider,
    ILogger? logger = null) : IEnumerateItemsQuery<TModel>
{
    private readonly IDeliveryApi _api = api;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly ContentItemMapper _contentItemMapper = contentItemMapper;
    private readonly ITypeProvider _typeProvider = typeProvider;
    private readonly ILogger? _logger = logger;
    private EnumItemsParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private readonly List<KeyValuePair<string, string>> _serializedFilters = [];
    private string? _continuationToken;
    private bool _typeFilterApplied;
    private static bool IsDynamicModel => ModelTypeHelper.IsDynamic<TModel>();

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
        var filterBuilder = new ItemsFilterBuilder(_serializedFilters);
        build(filterBuilder);
        return this;
    }

    private void ApplyGenericTypeFilter()
    {
        if (_typeFilterApplied)
            return;
        _typeFilterApplied = true;

        if (IsDynamicModel)
            return;

        var codename = _typeProvider.GetCodename(typeof(TModel));

        if (string.IsNullOrEmpty(codename))
        {
            if (_logger is not null)
            {
                LoggerMessages.GenericQueryTypeCodenameNotFound(_logger, typeof(TModel).Name);
            }
            return;
        }

        _serializedFilters.Add(new KeyValuePair<string, string>(
            FilterPath.System("type") + FilterSuffix.Eq,
            FilterValueSerializer.Serialize(codename)));
    }

    public async Task<IDeliveryResult<IDeliveryItemsFeedResponse<TModel>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        ApplyGenericTypeFilter();

        var wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();

        var resp = await _api
            .GetItemsFeedInternalAsync<TModel>(
                _params,
                FilterQueryParams.ToQueryDictionary(_serializedFilters),
                _continuationToken,
                wait,
                cancellationToken)
            .ConfigureAwait(false);

        var deliveryResult = await resp.ToDeliveryResultAsync(_logger).ConfigureAwait(false);

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

        if (!IsDynamicModel)
        {
            foreach (var item in content.Items)
            {
                await _contentItemMapper.CompleteItemAsync(item, content.ModularContent, null, cancellationToken).ConfigureAwait(false);
            }
        }

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
        return string.IsNullOrEmpty(continuationToken)
            ? null
            : (async (ct) =>
        {
            var nextQuery = new EnumerateItemsQuery<TModel>(_api, _getDefaultWaitForNewContent, _contentItemMapper, _typeProvider, _logger)
            {
                _params = _params,
                _waitForLoadingNewContentOverride = _waitForLoadingNewContentOverride,
                _continuationToken = continuationToken,
                _typeFilterApplied = _typeFilterApplied
            };

            foreach (var filter in _serializedFilters)
            {
                nextQuery._serializedFilters.Add(filter);
            }

            return await nextQuery.ExecuteAsync(ct).ConfigureAwait(false);
        });
    }

    public async IAsyncEnumerable<IContentItem<TModel>> EnumerateItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ApplyGenericTypeFilter();

        if (_logger is not null)
            LoggerMessages.PaginationStarted(_logger, "ItemsFeed");

        var wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        string? token = null;
        var pageCount = 0;
        var totalItems = 0;

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
                if (_logger is not null)
                    LoggerMessages.PaginationStoppedEarly(_logger, "ItemsFeed");
                yield break;
            }

            pageCount++;

            foreach (var item in resp.Content.Items)
            {
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
                if (_logger is not null)
                    LoggerMessages.PaginationCompleted(_logger, "ItemsFeed", pageCount, totalItems);
                yield break;
            }
        }
    }
}
