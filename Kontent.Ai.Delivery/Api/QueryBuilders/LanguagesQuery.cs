using System.Diagnostics;
using System.Net;
using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;
using Kontent.Ai.Delivery.Languages;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ILanguagesQuery"/>
internal sealed class LanguagesQuery(
    IDeliveryApi api,
    ILogger? logger = null) : ILanguagesQuery
{
    private LanguagesParams _params = new();
    private bool _waitForLoadingNewContent;

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
        _waitForLoadingNewContent = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IDeliveryLanguageListingResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        LogQueryStarting();
        var stopwatch = StartTimingIfEnabled();
        return await ExecuteWithoutCacheAsync(stopwatch, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IDeliveryResult<IDeliveryLanguageListingResponse>> ExecuteWithoutCacheAsync(
        Stopwatch? stopwatch,
        CancellationToken cancellationToken)
    {
        var deliveryResult = await FetchFromApiAsync(cancellationToken).ConfigureAwait(false);
        if (!deliveryResult.IsSuccess)
        {
            LogQueryFailed(deliveryResult);
            LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false, deliveryResult.HasStaleContent);
            return CreateFailureResult(deliveryResult);
        }

        LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false, deliveryResult.HasStaleContent);
        return WrapSuccess(WithNextPageFetcher(deliveryResult.Value), deliveryResult);
    }

    private async Task<IDeliveryResult<DeliveryLanguageListingResponse>> FetchFromApiAsync(CancellationToken cancellationToken)
    {
        bool? waitForLoadingNewContent = _waitForLoadingNewContent ? true : null;
        var response = await api.GetLanguagesInternalAsync(_params, waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync(logger).ConfigureAwait(false);
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
        var parametersSnapshot = _params;
        var waitForLoadingSnapshot = _waitForLoadingNewContent;

        return ct => CreateNextPageQuery(nextSkip, parametersSnapshot, waitForLoadingSnapshot).ExecuteAsync(ct);
    }

    private LanguagesQuery CreateNextPageQuery(int nextSkip, LanguagesParams parametersSnapshot, bool waitForLoadingSnapshot)
        => new(api, logger)
        {
            _params = parametersSnapshot with { Skip = nextSkip },
            _waitForLoadingNewContent = waitForLoadingSnapshot
        };

    private void LogQueryStarting()
    {
        if (logger is not null)
            LoggerMessages.QueryStarting(logger, "Languages", "list");
    }

    private Stopwatch? StartTimingIfEnabled() =>
        logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

    private void LogQueryFailed(IDeliveryResult<DeliveryLanguageListingResponse> deliveryResult)
    {
        if (logger is not null)
        {
            LoggerMessages.QueryFailed(logger, "Languages", "list", deliveryResult.StatusCode,
                deliveryResult.Error?.Message, exception: null);
        }
    }

    private void LogQueryCompleted(Stopwatch? stopwatch, HttpStatusCode statusCode, bool cacheHit, bool hasStaleContent = false)
    {
        if (logger is null)
            return;
        stopwatch?.Stop();
        if (hasStaleContent)
            LoggerMessages.QueryStaleContent(logger, "list");
        LoggerMessages.QueryCompleted(logger, "Languages", "list",
            stopwatch?.ElapsedMilliseconds ?? 0, statusCode, cacheHit);
    }
}
