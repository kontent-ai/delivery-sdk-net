using System.Diagnostics;
using System.Net;
using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.ContentTypes;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITypeQuery"/>
internal sealed class TypeQuery(
    IDeliveryApi api,
    string codename,
    Func<bool?> getDefaultWaitForNewContent,
    IDeliveryCacheManager? cacheManager,
    ILogger? logger = null) : ITypeQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private SingleTypeParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;
    private readonly ILogger? _logger = logger;

    public ITypeQuery WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public ITypeQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IContentType>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        LogQueryStarting();
        var stopwatch = StartTimingIfEnabled();

        if (_cacheManager is not null)
        {
            var cacheKey = CacheKeyBuilder.BuildTypeKey(_codename, _params);
            IDeliveryResult<IContentType>? apiResult = null;

            var cacheResult = await QueryCacheHelper.GetOrFetchAsync(
                _cacheManager,
                cacheKey,
                async ct =>
                {
                    apiResult = await FetchFromApiAsync(ct).ConfigureAwait(false);
                    if (!apiResult.IsSuccess)
                        return (null, Array.Empty<string>());
                    return ((ContentType)apiResult.Value, Array.Empty<string>());
                },
                _logger,
                cancellationToken).ConfigureAwait(false);

            if (cacheResult.IsCacheHit)
            {
                LogQueryCompleted(stopwatch, HttpStatusCode.OK, cacheHit: true);
                return DeliveryResult.CacheHit<IContentType>(cacheResult.Value!);
            }

            LogQueryCompleted(stopwatch, apiResult!.StatusCode, cacheHit: false);
            return apiResult!;
        }

        var deliveryResult = await FetchFromApiAsync(cancellationToken).ConfigureAwait(false);
        LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false);
        return deliveryResult;
    }

    private async Task<IDeliveryResult<IContentType>> FetchFromApiAsync(CancellationToken cancellationToken)
    {
        var wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var response = await _api.GetTypeInternalAsync(_codename, _params, wait, cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync(_logger).ConfigureAwait(false);
    }

    private void LogQueryStarting()
    {
        if (_logger is not null)
            LoggerMessages.QueryStarting(_logger, "Type", _codename);
    }

    private Stopwatch? StartTimingIfEnabled() =>
        _logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

    private void LogQueryCompleted(Stopwatch? stopwatch, HttpStatusCode statusCode, bool cacheHit)
    {
        if (_logger is null)
            return;
        stopwatch?.Stop();
        LoggerMessages.QueryCompleted(_logger, "Type", _codename,
            stopwatch?.ElapsedMilliseconds ?? 0, statusCode, cacheHit);
    }
}
