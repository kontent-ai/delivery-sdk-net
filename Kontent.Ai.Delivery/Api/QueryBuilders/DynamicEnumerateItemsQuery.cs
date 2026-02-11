using System.Runtime.CompilerServices;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IDynamicEnumerateItemsQuery"/>
internal sealed class DynamicEnumerateItemsQuery(
    IDeliveryApi api,
    ContentItemMapper contentItemMapper,
    ITypeProvider typeProvider,
    ILogger? logger = null) : IDynamicEnumerateItemsQuery
{
    private readonly ContentItemMapper _contentItemMapper = contentItemMapper;
    private readonly ILogger? _logger = logger;
    private readonly EnumerateItemsQuery<IDynamicElements> _inner = new(
        api,
        contentItemMapper,
        typeProvider,
        logger);

    public IDynamicEnumerateItemsQuery WithLanguage(string languageCodename, LanguageFallbackMode languageFallbackMode = LanguageFallbackMode.Enabled)
    {
        _inner.WithLanguage(languageCodename, languageFallbackMode);
        return this;
    }

    public IDynamicEnumerateItemsQuery WithElements(params string[] elementCodenames)
    {
        _inner.WithElements(elementCodenames);
        return this;
    }

    public IDynamicEnumerateItemsQuery OrderBy(string elementOrAttributePath, OrderingMode orderingMode = OrderingMode.Ascending)
    {
        _inner.OrderBy(elementOrAttributePath, orderingMode);
        return this;
    }

    public IDynamicEnumerateItemsQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _inner.WaitForLoadingNewContent(enabled);
        return this;
    }

    public IDynamicEnumerateItemsQuery Where(Func<IItemsFilterBuilder, IItemsFilterBuilder> build)
    {
        _inner.Where(build);
        return this;
    }

    public async Task<IDeliveryResult<IDeliveryItemsFeedResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var deliveryResult = await _inner.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        if (!deliveryResult.IsSuccess)
        {
            return DeliveryResult.FailureFrom<IDeliveryItemsFeedResponse, IDeliveryItemsFeedResponse<IDynamicElements>>(deliveryResult);
        }

        var response = await ConvertResponseAsync(deliveryResult.Value, cancellationToken).ConfigureAwait(false);

        return DeliveryResult.SuccessFrom<IDeliveryItemsFeedResponse, IDeliveryItemsFeedResponse<IDynamicElements>>(response, deliveryResult);
    }

    private async Task<DynamicDeliveryItemsFeedResponse> ConvertResponseAsync(
        IDeliveryItemsFeedResponse<IDynamicElements> response,
        CancellationToken cancellationToken)
    {
        var runtimeTypedItems = await _contentItemMapper.RuntimeTypeItemsAsync(
            response.Items,
            response.ModularContent,
            cancellationToken).ConfigureAwait(false);

        return new DynamicDeliveryItemsFeedResponse
        {
            Items = runtimeTypedItems,
            ModularContent = response.ModularContent,
            ContinuationToken = GetContinuationToken(response),
            NextPageFetcher = CreateNextPageFetcher(response)
        };
    }

    private static string? GetContinuationToken(IDeliveryItemsFeedResponse<IDynamicElements> response)
    {
        return response switch
        {
            DeliveryItemsFeedResponse<IDynamicElements> concrete => concrete.ContinuationToken,
            _ => response.HasNextPage ? "__next_page__" : null
        };
    }

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemsFeedResponse>>>? CreateNextPageFetcher(
        IDeliveryItemsFeedResponse<IDynamicElements> page)
    {
        if (!page.HasNextPage)
            return null;

        return ct => FetchNextPageAndConvertAsync(page, ct);
    }

    private async Task<IDeliveryResult<IDeliveryItemsFeedResponse>> FetchNextPageAndConvertAsync(
        IDeliveryItemsFeedResponse<IDynamicElements> currentPage,
        CancellationToken cancellationToken)
    {
        var nextPageResult = await currentPage.FetchNextPageAsync(cancellationToken).ConfigureAwait(false);
        if (nextPageResult is null)
            throw new InvalidOperationException("The current feed page indicated a next page, but fetching it returned null.");

        if (!nextPageResult.IsSuccess)
        {
            return DeliveryResult.FailureFrom<IDeliveryItemsFeedResponse, IDeliveryItemsFeedResponse<IDynamicElements>>(nextPageResult);
        }

        var response = await ConvertResponseAsync(nextPageResult.Value, cancellationToken).ConfigureAwait(false);
        return DeliveryResult.SuccessFrom<IDeliveryItemsFeedResponse, IDeliveryItemsFeedResponse<IDynamicElements>>(response, nextPageResult);
    }

    public async IAsyncEnumerable<IContentItem> EnumerateItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_logger is not null)
            LoggerMessages.PaginationStarted(_logger, "ItemsFeed (dynamic)");

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
                    LoggerMessages.PaginationCompleted(_logger, "ItemsFeed (dynamic)", pageCount, totalItems);
                yield break;
            }

            var nextPageResult = await pageResult.Value.FetchNextPageAsync(cancellationToken).ConfigureAwait(false);
            if (nextPageResult is not { IsSuccess: true })
            {
                if (_logger is not null)
                    LoggerMessages.PaginationStoppedEarly(_logger, "ItemsFeed (dynamic)");
                yield break;
            }

            pageResult = nextPageResult;
        }

        if (_logger is not null)
            LoggerMessages.PaginationStoppedEarly(_logger, "ItemsFeed (dynamic)");
    }
}
