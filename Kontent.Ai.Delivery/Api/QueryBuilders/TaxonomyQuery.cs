using System.Diagnostics;
using System.Net;
using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.Logging;
using Kontent.Ai.Delivery.TaxonomyGroups;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITaxonomyQuery"/>
internal sealed class TaxonomyQuery(
    IDeliveryApi api,
    string codename,
    IDeliveryCacheManager? cacheManager,
    ILogger? logger = null) : ITaxonomyQuery, ICacheExpirationConfigurable
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private bool _waitForLoadingNewContent;
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;
    private readonly ILogger? _logger = logger;
    public TimeSpan? CacheExpiration { get; set; }

    public ITaxonomyQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContent = enabled;
        return this;
    }

    public async Task<IDeliveryResult<ITaxonomyGroup>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        LogQueryStarting();
        var stopwatch = StartTimingIfEnabled();
        bool? waitForLoadingNewContent = _waitForLoadingNewContent ? true : null;
        var shouldBypassCache = _waitForLoadingNewContent;

        return _cacheManager is not null && !shouldBypassCache
            ? await ExecuteWithCacheAsync(
                _cacheManager,
                stopwatch,
                waitForLoadingNewContent,
                cancellationToken).ConfigureAwait(false)
            : await ExecuteWithoutCacheAsync(stopwatch, waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IDeliveryResult<ITaxonomyGroup>> ExecuteWithCacheAsync(
        IDeliveryCacheManager cacheManager,
        Stopwatch? stopwatch,
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildTaxonomyKey(_codename);
        var (cacheResult, apiResult) = await FetchWithCacheAsync(
            cacheManager,
            cacheKey,
            waitForLoadingNewContent,
            cancellationToken).ConfigureAwait(false);

        if (QueryExecutionResultHelper.TryGetCacheHitValue(cacheResult, out var cachedTaxonomy))
        {
            LogQueryCompleted(stopwatch, HttpStatusCode.OK, cacheHit: true);
            return DeliveryResult.CacheHit<ITaxonomyGroup>(cachedTaxonomy);
        }

        apiResult = QueryExecutionResultHelper.EnsureApiResult(apiResult, "Taxonomy", _codename);

        if (!apiResult.IsSuccess)
            LogQueryFailed(apiResult);

        LogQueryCompleted(stopwatch, apiResult.StatusCode, cacheHit: false, apiResult.HasStaleContent);
        return apiResult;
    }

    private async Task<IDeliveryResult<ITaxonomyGroup>> ExecuteWithoutCacheAsync(
        Stopwatch? stopwatch,
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var deliveryResult = await FetchFromApiAsync(waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
        if (!deliveryResult.IsSuccess)
            LogQueryFailed(deliveryResult);

        LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false, deliveryResult.HasStaleContent);
        return deliveryResult;
    }

    private async Task<(CacheFetchResult<TaxonomyGroup> CacheResult, IDeliveryResult<ITaxonomyGroup>? ApiResult)> FetchWithCacheAsync(
        IDeliveryCacheManager cacheManager,
        string cacheKey,
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        IDeliveryResult<ITaxonomyGroup>? apiResult = null;

        var cacheResult = await QueryCacheHelper.GetOrFetchAsync(
            cacheManager,
            cacheKey,
            async ct =>
            {
                apiResult = await FetchFromApiAsync(waitForLoadingNewContent, ct).ConfigureAwait(false);
                if (!apiResult.IsSuccess)
                    return (null, Array.Empty<string>());

                var dependency = CacheDependencyKeyBuilder.BuildTaxonomyDependencyKey(apiResult.Value.System.Codename);
                var dependencies = dependency is null ? Array.Empty<string>() : [dependency];

                return ((TaxonomyGroup)apiResult.Value, dependencies);
            },
            CacheExpiration,
            _logger,
            cancellationToken).ConfigureAwait(false);

        return (cacheResult, apiResult);
    }

    private async Task<IDeliveryResult<ITaxonomyGroup>> FetchFromApiAsync(
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var response = await _api.GetTaxonomyInternalAsync(_codename, waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync(_logger).ConfigureAwait(false);
    }

    private void LogQueryStarting()
    {
        if (_logger is not null)
            LoggerMessages.QueryStarting(_logger, "Taxonomy", _codename);
    }

    private Stopwatch? StartTimingIfEnabled() =>
        _logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

    private void LogQueryFailed(IDeliveryResult<ITaxonomyGroup> deliveryResult)
    {
        if (_logger is not null)
        {
            LoggerMessages.QueryFailed(_logger, "Taxonomy", _codename, deliveryResult.StatusCode,
                deliveryResult.Error?.Message, exception: null);
        }
    }

    private void LogQueryCompleted(Stopwatch? stopwatch, HttpStatusCode statusCode, bool cacheHit, bool hasStaleContent = false)
    {
        if (_logger is null)
            return;
        stopwatch?.Stop();
        if (hasStaleContent)
            LoggerMessages.QueryStaleContent(_logger, _codename);
        LoggerMessages.QueryCompleted(_logger, "Taxonomy", _codename,
            stopwatch?.ElapsedMilliseconds ?? 0, statusCode, cacheHit);
    }
}
