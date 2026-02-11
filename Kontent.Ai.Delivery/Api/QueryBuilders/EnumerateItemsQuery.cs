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
    private readonly SerializedFilterCollection _serializedFilters = [];
    private string? _continuationToken;
    private bool _typeFilterApplied;
    private static bool IsDynamicModel => ModelTypeHelper.IsDynamic<TModel>();

    public IEnumerateItemsQuery<TModel> WithLanguage(string languageCodename, LanguageFallbackMode languageFallbackMode = LanguageFallbackMode.Enabled)
    {
        _params = _params with { Language = languageCodename };
        if (languageFallbackMode == LanguageFallbackMode.Disabled)
        {
            SystemFilterHelpers.AddSystemLanguageFilter(_serializedFilters, languageCodename);
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

        SystemFilterHelpers.AddGenericTypeFilter<TModel>(_serializedFilters, _typeProvider, _logger);
    }

    public async Task<IDeliveryResult<IDeliveryItemsFeedResponse<TModel>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        ApplyGenericTypeFilter();

        var (deliveryResult, continuationToken) = await FetchFeedPageAsync(_continuationToken, cancellationToken).ConfigureAwait(false);

        if (!deliveryResult.IsSuccess)
        {
            return CreateFailureResult(deliveryResult);
        }

        var response = await PreparePageAsync(deliveryResult.Value, continuationToken, cancellationToken).ConfigureAwait(false);
        return WrapSuccess(response, deliveryResult);
    }

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemsFeedResponse<TModel>>>>? CreateNextPageFetcher(string? continuationToken)
    {
        if (string.IsNullOrEmpty(continuationToken))
            return null;

        return ct => CreateContinuationQuery(continuationToken).ExecuteAsync(ct);
    }

    public async IAsyncEnumerable<IContentItem<TModel>> EnumerateItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_logger is not null)
            LoggerMessages.PaginationStarted(_logger, "ItemsFeed");

        var pageResult = await ExecuteAsync(cancellationToken).ConfigureAwait(false);
        var pageCount = 0;
        var totalItems = 0;

        while (pageResult is { IsSuccess: true })
        {
            pageCount++;

            foreach (var item in pageResult.Value.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                totalItems++;
                yield return item;
            }

            if (!pageResult.Value.HasNextPage)
            {
                if (_logger is not null)
                    LoggerMessages.PaginationCompleted(_logger, "ItemsFeed", pageCount, totalItems);
                yield break;
            }

            var nextPageResult = await pageResult.Value.FetchNextPageAsync(cancellationToken).ConfigureAwait(false);
            if (nextPageResult is not { IsSuccess: true })
            {
                if (_logger is not null)
                    LoggerMessages.PaginationStoppedEarly(_logger, "ItemsFeed");
                yield break;
            }

            pageResult = nextPageResult;
        }

        if (_logger is not null)
            LoggerMessages.PaginationStoppedEarly(_logger, "ItemsFeed");
    }

    private async Task<(IDeliveryResult<DeliveryItemsFeedResponse<TModel>> DeliveryResult, string? ContinuationToken)> FetchFeedPageAsync(
        string? continuationToken,
        CancellationToken cancellationToken)
    {
        var wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var resp = await _api
            .GetItemsFeedInternalAsync<TModel>(
                _params,
                _serializedFilters.ToQueryDictionary(),
                continuationToken,
                wait,
                cancellationToken)
            .ConfigureAwait(false);

        var deliveryResult = await resp.ToDeliveryResultAsync(_logger).ConfigureAwait(false);
        return (deliveryResult, resp.Continuation());
    }

    private async Task<DeliveryItemsFeedResponse<TModel>> PreparePageAsync(
        DeliveryItemsFeedResponse<TModel> content,
        string? continuationToken,
        CancellationToken cancellationToken)
    {
        if (!IsDynamicModel)
        {
            foreach (var item in content.Items)
            {
                await _contentItemMapper.CompleteItemAsync(item, content.ModularContent, null, cancellationToken).ConfigureAwait(false);
            }
        }

        return content with
        {
            ContinuationToken = continuationToken,
            NextPageFetcher = CreateNextPageFetcher(continuationToken)
        };
    }

    private EnumerateItemsQuery<TModel> CreateContinuationQuery(string continuationToken)
    {
        var nextQuery = new EnumerateItemsQuery<TModel>(_api, _getDefaultWaitForNewContent, _contentItemMapper, _typeProvider, _logger)
        {
            _params = _params,
            _waitForLoadingNewContentOverride = _waitForLoadingNewContentOverride,
            _continuationToken = continuationToken,
            _typeFilterApplied = _typeFilterApplied
        };

        nextQuery._serializedFilters.CopyFrom(_serializedFilters);
        return nextQuery;
    }

    private static IDeliveryResult<IDeliveryItemsFeedResponse<TModel>> WrapSuccess(
        DeliveryItemsFeedResponse<TModel> response,
        IDeliveryResult<DeliveryItemsFeedResponse<TModel>> apiResult) =>
        DeliveryResult.SuccessFrom<IDeliveryItemsFeedResponse<TModel>, DeliveryItemsFeedResponse<TModel>>(response, apiResult);

    private static IDeliveryResult<IDeliveryItemsFeedResponse<TModel>> CreateFailureResult(
        IDeliveryResult<DeliveryItemsFeedResponse<TModel>> deliveryResult) =>
        DeliveryResult.FailureFrom<IDeliveryItemsFeedResponse<TModel>, DeliveryItemsFeedResponse<TModel>>(deliveryResult);
}
