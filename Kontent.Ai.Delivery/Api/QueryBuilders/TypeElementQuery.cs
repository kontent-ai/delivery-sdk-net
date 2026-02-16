using System.Diagnostics;
using System.Net;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITypeElementQuery"/>
internal sealed class TypeElementQuery(
    IDeliveryApi api,
    string contentTypeCodename,
    string elementCodename,
    ILogger? logger = null) : ITypeElementQuery
{
    private bool _waitForLoadingNewContent;

    public ITypeElementQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContent = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IContentElement>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        LogQueryStarting();
        var stopwatch = StartTimingIfEnabled();
        var deliveryResult = await FetchFromApiAsync(cancellationToken).ConfigureAwait(false);
        if (!deliveryResult.IsSuccess)
            LogQueryFailed(deliveryResult);

        LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false, deliveryResult.HasStaleContent);
        return deliveryResult;
    }

    private async Task<IDeliveryResult<IContentElement>> FetchFromApiAsync(CancellationToken cancellationToken)
    {
        bool? waitForLoadingNewContent = _waitForLoadingNewContent ? true : null;
        var response = await api.GetContentElementInternalAsync(contentTypeCodename, elementCodename, waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync(logger).ConfigureAwait(false);
    }

    private void LogQueryStarting()
    {
        if (logger is not null)
            LoggerMessages.QueryStarting(logger, "TypeElement", $"{contentTypeCodename}/{elementCodename}");
    }

    private Stopwatch? StartTimingIfEnabled() =>
        logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

    private void LogQueryFailed(IDeliveryResult<IContentElement> deliveryResult)
    {
        if (logger is not null)
        {
            LoggerMessages.QueryFailed(logger, "TypeElement", $"{contentTypeCodename}/{elementCodename}", deliveryResult.StatusCode,
                deliveryResult.Error?.Message, exception: null);
        }
    }

    private void LogQueryCompleted(Stopwatch? stopwatch, HttpStatusCode statusCode, bool cacheHit, bool hasStaleContent = false)
    {
        if (logger is null)
            return;
        stopwatch?.Stop();
        if (hasStaleContent)
            LoggerMessages.QueryStaleContent(logger, $"{contentTypeCodename}/{elementCodename}");
        LoggerMessages.QueryCompleted(logger, "TypeElement", $"{contentTypeCodename}/{elementCodename}",
            stopwatch?.ElapsedMilliseconds ?? 0, statusCode, cacheHit);
    }
}
