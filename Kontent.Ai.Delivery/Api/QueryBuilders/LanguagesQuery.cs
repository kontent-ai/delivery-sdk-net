using Kontent.Ai.Delivery.Extensions;
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
    {
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var response = await _api.GetLanguagesInternalAsync(_params, wait).ConfigureAwait(false);
        var deliveryResult = await response.ToDeliveryResultAsync().ConfigureAwait(false);

        if (!deliveryResult.IsSuccess)
        {
            return DeliveryResult.Failure<IDeliveryLanguageListingResponse>(
                deliveryResult.RequestUrl ?? string.Empty,
                deliveryResult.StatusCode,
                deliveryResult.Error,
                deliveryResult.ResponseHeaders);
        }

        var resp = deliveryResult.Value;

        // Build response with next page fetcher
        var responseWithFetcher = resp with
        {
            NextPageFetcher = CreateNextPageFetcher(resp.Pagination)
        };

        return DeliveryResult.Success<IDeliveryLanguageListingResponse>(
            responseWithFetcher,
            deliveryResult.RequestUrl ?? string.Empty,
            deliveryResult.StatusCode,
            deliveryResult.HasStaleContent,
            deliveryResult.ContinuationToken,
            deliveryResult.ResponseHeaders);
    }

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryLanguageListingResponse>>>? CreateNextPageFetcher(IPagination pagination)
    {
        if (string.IsNullOrEmpty(pagination.NextPageUrl))
            return null;

        // Calculate next skip value
        var nextSkip = pagination.Skip + pagination.Count;

        return async (ct) =>
        {
            var nextQuery = new LanguagesQuery(_api, _getDefaultWaitForNewContent)
            {
                _params = _params with { Skip = nextSkip },
                _waitForLoadingNewContentOverride = _waitForLoadingNewContentOverride
            };

            return await nextQuery.ExecuteAsync(ct).ConfigureAwait(false);
        };
    }
}