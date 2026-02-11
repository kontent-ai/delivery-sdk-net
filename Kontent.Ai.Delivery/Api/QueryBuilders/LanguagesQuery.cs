using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;
using Kontent.Ai.Delivery.Languages;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ILanguagesQuery"/>
internal sealed class LanguagesQuery(IDeliveryApi api, Func<bool?> getDefaultWaitForNewContent) : ILanguagesQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private LanguagesParams _params = new();
    private bool? _waitForLoadingNewContentOverride;

    public ILanguagesQuery OrderBy(string elementOrAttributePath, OrderingMode orderingMode = OrderingMode.Ascending)
    {
        _params = _params with
        {
            OrderBy = orderingMode == OrderingMode.Ascending
                ? $"{elementOrAttributePath}[asc]"
                : $"{elementOrAttributePath}[desc]"
        };
        return this;
    }

    public ILanguagesQuery Skip(int skip)
    {
        _params = _params with { Skip = skip };
        return this;
    }

    public ILanguagesQuery Limit(int limit)
    {
        _params = _params with { Limit = limit };
        return this;
    }

    public ILanguagesQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IDeliveryLanguageListingResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
        => await ExecuteWithoutCacheAsync(cancellationToken).ConfigureAwait(false);

    private async Task<IDeliveryResult<IDeliveryLanguageListingResponse>> ExecuteWithoutCacheAsync(CancellationToken cancellationToken)
    {
        var deliveryResult = await FetchFromApiAsync(cancellationToken).ConfigureAwait(false);
        return deliveryResult.IsSuccess
            ? WrapSuccess(WithNextPageFetcher(deliveryResult.Value), deliveryResult)
            : CreateFailureResult(deliveryResult);
    }

    private async Task<IDeliveryResult<DeliveryLanguageListingResponse>> FetchFromApiAsync(CancellationToken cancellationToken)
    {
        var waitForLoadingNewContent = WaitForLoadingNewContentHelper.ResolveHeaderValue(
            _waitForLoadingNewContentOverride,
            _getDefaultWaitForNewContent());
        var response = await _api.GetLanguagesInternalAsync(_params, waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync().ConfigureAwait(false);
    }

    private static IDeliveryResult<IDeliveryLanguageListingResponse> WrapSuccess(
        DeliveryLanguageListingResponse response,
        IDeliveryResult<DeliveryLanguageListingResponse> apiResult)
        => DeliveryResult.SuccessFrom<IDeliveryLanguageListingResponse, DeliveryLanguageListingResponse>(response, apiResult);

    private static IDeliveryResult<IDeliveryLanguageListingResponse> CreateFailureResult(
        IDeliveryResult<DeliveryLanguageListingResponse> deliveryResult)
        => DeliveryResult.FailureFrom<IDeliveryLanguageListingResponse, DeliveryLanguageListingResponse>(deliveryResult);

    private DeliveryLanguageListingResponse WithNextPageFetcher(DeliveryLanguageListingResponse response)
        => response with { NextPageFetcher = CreateNextPageFetcher(response.Pagination) };

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryLanguageListingResponse>>>? CreateNextPageFetcher(IPagination pagination)
    {
        if (string.IsNullOrEmpty(pagination.NextPageUrl))
            return null;

        var nextSkip = OffsetPaginationHelper.GetNextSkip(pagination);

        return ct => CreateNextPageQuery(nextSkip).ExecuteAsync(ct);
    }

    private LanguagesQuery CreateNextPageQuery(int nextSkip)
        => new(_api, _getDefaultWaitForNewContent)
        {
            _params = _params with { Skip = nextSkip },
            _waitForLoadingNewContentOverride = _waitForLoadingNewContentOverride
        };
}
