using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITypeElementQuery"/>
internal sealed class TypeElementQuery(
    IDeliveryApi api,
    string contentTypeCodename,
    string elementCodename,
    ILogger? logger = null) : ITypeElementQuery
{
    private readonly QueryLoggingHelper _log = new(logger, "TypeElement", $"{contentTypeCodename}/{elementCodename}");
    private bool _waitForLoadingNewContent;

    public ITypeElementQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContent = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IContentElement>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _log.LogQueryStarting();
        var stopwatch = _log.StartTimingIfEnabled();
        var deliveryResult = await FetchFromApiAsync(cancellationToken).ConfigureAwait(false);
        if (!deliveryResult.IsSuccess)
            _log.LogQueryFailed(deliveryResult.StatusCode, deliveryResult.Error?.Message);

        _log.LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false, deliveryResult.HasStaleContent);
        return deliveryResult;
    }

    private async Task<IDeliveryResult<IContentElement>> FetchFromApiAsync(CancellationToken cancellationToken)
    {
        bool? waitForLoadingNewContent = _waitForLoadingNewContent ? true : null;
        var response = await api.GetContentElementInternalAsync(contentTypeCodename, elementCodename, waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync(logger).ConfigureAwait(false);
    }
}
