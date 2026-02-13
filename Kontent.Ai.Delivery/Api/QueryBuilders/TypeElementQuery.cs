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
    private readonly IDeliveryApi _api = api;
    private readonly string _type = contentTypeCodename;
    private readonly string _element = elementCodename;
    private readonly ILogger? _logger = logger;
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
        var response = await _api.GetContentElementInternalAsync(_type, _element, waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync(_logger).ConfigureAwait(false);
    }

    private void LogQueryStarting()
    {
        if (_logger is not null)
            LoggerMessages.QueryStarting(_logger, "TypeElement", $"{_type}/{_element}");
    }

    private Stopwatch? StartTimingIfEnabled() =>
        _logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

    private void LogQueryFailed(IDeliveryResult<IContentElement> deliveryResult)
    {
        if (_logger is not null)
        {
            LoggerMessages.QueryFailed(_logger, "TypeElement", $"{_type}/{_element}", deliveryResult.StatusCode,
                deliveryResult.Error?.Message, exception: null);
        }
    }

    private void LogQueryCompleted(Stopwatch? stopwatch, HttpStatusCode statusCode, bool cacheHit, bool hasStaleContent = false)
    {
        if (_logger is null)
            return;
        stopwatch?.Stop();
        if (hasStaleContent)
            LoggerMessages.QueryStaleContent(_logger, $"{_type}/{_element}");
        LoggerMessages.QueryCompleted(_logger, "TypeElement", $"{_type}/{_element}",
            stopwatch?.ElapsedMilliseconds ?? 0, statusCode, cacheHit);
    }
}
