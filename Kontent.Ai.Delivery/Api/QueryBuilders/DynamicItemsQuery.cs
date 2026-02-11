using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Kontent.Ai.Delivery.SharedModels;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <summary>
/// Concrete implementation of <see cref="IDynamicItemsQuery"/> using the modernized Result pattern.
/// Supports runtime type resolution via <see cref="ContentItemMapper.TryRuntimeTypeItemAsync"/>.
/// </summary>
/// <remarks>
/// Cache support is intentionally omitted as the runtime-typed result type varies per item.
/// Use strongly-typed queries (<see cref="IItemsQuery{TModel}"/>) for cacheable results.
/// </remarks>
internal sealed class DynamicItemsQuery(
    IDeliveryApi api,
    Func<bool?> getDefaultWaitForNewContent,
    ContentItemMapper contentItemMapper,
    IContentDeserializer contentDeserializer,
    ITypeProvider typeProvider,
    ILogger? logger = null) : IDynamicItemsQuery
{
    private readonly ContentItemMapper _contentItemMapper = contentItemMapper;
    private readonly ItemsQuery<IDynamicElements> _inner = new(
        api,
        getDefaultWaitForNewContent,
        contentItemMapper,
        contentDeserializer,
        typeProvider,
        cacheManager: null,
        logger);

    public IDynamicItemsQuery WithLanguage(string languageCodename, LanguageFallbackMode languageFallbackMode = LanguageFallbackMode.Enabled)
    {
        _inner.WithLanguage(languageCodename, languageFallbackMode);
        return this;
    }

    public IDynamicItemsQuery WithElements(params string[] elementCodenames)
    {
        _inner.WithElements(elementCodenames);
        return this;
    }

    public IDynamicItemsQuery WithoutElements(params string[] elementCodenames)
    {
        _inner.WithoutElements(elementCodenames);
        return this;
    }

    public IDynamicItemsQuery Depth(int depth)
    {
        _inner.Depth(depth);
        return this;
    }

    public IDynamicItemsQuery Skip(int skip)
    {
        _inner.Skip(skip);
        return this;
    }

    public IDynamicItemsQuery Limit(int limit)
    {
        _inner.Limit(limit);
        return this;
    }

    public IDynamicItemsQuery OrderBy(string elementOrAttributePath, OrderingMode orderingMode = OrderingMode.Ascending)
    {
        _inner.OrderBy(elementOrAttributePath, orderingMode);
        return this;
    }

    public IDynamicItemsQuery WithTotalCount()
    {
        _inner.WithTotalCount();
        return this;
    }

    public IDynamicItemsQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _inner.WaitForLoadingNewContent(enabled);
        return this;
    }

    public IDynamicItemsQuery Where(Func<IItemsFilterBuilder, IItemsFilterBuilder> build)
    {
        _inner.Where(build);
        return this;
    }

    public async Task<IDeliveryResult<IDeliveryItemListingResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var deliveryResult = await _inner.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        if (!deliveryResult.IsSuccess)
        {
            return DeliveryResult.FailureFrom<IDeliveryItemListingResponse, IDeliveryItemListingResponse<IDynamicElements>>(deliveryResult);
        }

        var response = await ConvertResponseAsync(deliveryResult.Value, cancellationToken).ConfigureAwait(false);

        return DeliveryResult.SuccessFrom<IDeliveryItemListingResponse, IDeliveryItemListingResponse<IDynamicElements>>(response, deliveryResult);
    }

    private async Task<DynamicDeliveryItemListingResponse> ConvertResponseAsync(
        IDeliveryItemListingResponse<IDynamicElements> response,
        CancellationToken cancellationToken)
    {
        var runtimeTypedItems = await _contentItemMapper.RuntimeTypeItemsAsync(
            response.Items,
            response.ModularContent,
            cancellationToken).ConfigureAwait(false);

        return new DynamicDeliveryItemListingResponse
        {
            Items = runtimeTypedItems,
            Pagination = ToPagination(response.Pagination),
            ModularContent = response.ModularContent,
            NextPageFetcher = CreateNextPageFetcher(response)
        };
    }

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemListingResponse>>>? CreateNextPageFetcher(
        IDeliveryItemListingResponse<IDynamicElements> page) => !page.HasNextPage ? null : (ct => FetchNextPageAndConvertAsync(page, ct));

    private async Task<IDeliveryResult<IDeliveryItemListingResponse>> FetchNextPageAndConvertAsync(
        IDeliveryItemListingResponse<IDynamicElements> currentPage,
        CancellationToken cancellationToken)
    {
        var nextPageResult = await currentPage.FetchNextPageAsync(cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("The current page indicated a next page, but fetching it returned null.");

        if (!nextPageResult.IsSuccess)
        {
            return DeliveryResult.FailureFrom<IDeliveryItemListingResponse, IDeliveryItemListingResponse<IDynamicElements>>(nextPageResult);
        }

        var response = await ConvertResponseAsync(nextPageResult.Value, cancellationToken).ConfigureAwait(false);

        return DeliveryResult.SuccessFrom<IDeliveryItemListingResponse, IDeliveryItemListingResponse<IDynamicElements>>(response, nextPageResult);
    }

    private static Pagination ToPagination(IPagination pagination) => new()
    {
        Skip = pagination.Skip,
        Limit = pagination.Limit,
        Count = pagination.Count,
        TotalCount = pagination.TotalCount,
        NextPageUrl = pagination.NextPageUrl
    };
}
