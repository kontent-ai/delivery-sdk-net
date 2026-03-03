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
    private bool _waitForLoadingNewContent;
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

        return cacheManager is not null && !shouldBypassCache
            ? await ExecuteWithCacheAsync(
                cacheManager,
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
        var cacheKey = CacheKeyBuilder.BuildTaxonomyKey(codename);
        IDeliveryResult<ITaxonomyGroup>? apiResult = null;
        var factoryInvoked = false;

        var cached = await cacheManager.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                factoryInvoked = true;
                apiResult = await FetchFromApiAsync(waitForLoadingNewContent, ct).ConfigureAwait(false);
                if (!apiResult.IsSuccess)
                    return null;

                var dependency = CacheDependencyKeyBuilder.BuildTaxonomyDependencyKey(apiResult.Value.System.Codename);
                var dependencies = dependency is null ? Array.Empty<string>() : new[] { dependency };

                return new CacheEntry<TaxonomyGroup>((TaxonomyGroup)apiResult.Value, dependencies);
            },
            CacheExpiration,
            cancellationToken).ConfigureAwait(false);

        // Cache hit (factory never called) or fail-safe served stale data after HTTP error
        if (cached is not null && (apiResult is null || !apiResult.IsSuccess))
        {
            LogQueryCompleted(stopwatch, HttpStatusCode.OK, cacheHit: true);
            var isFailSafe = factoryInvoked
                || (cacheManager is IFailSafeStateProvider failSafeProvider && failSafeProvider.IsFailSafeActive(cacheKey));

            return isFailSafe
                ? DeliveryResult.FailSafeHit<ITaxonomyGroup>(cached)
                : DeliveryResult.CacheHit<ITaxonomyGroup>(cached);
        }

        apiResult = QueryExecutionResultHelper.EnsureApiResult(apiResult, "Taxonomy", codename);

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

    private async Task<IDeliveryResult<ITaxonomyGroup>> FetchFromApiAsync(
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var response = await api.GetTaxonomyInternalAsync(codename, waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync(logger).ConfigureAwait(false);
    }

    private void LogQueryStarting()
    {
        if (logger is not null)
            LoggerMessages.QueryStarting(logger, "Taxonomy", codename);
    }

    private Stopwatch? StartTimingIfEnabled() =>
        logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

    private void LogQueryFailed(IDeliveryResult<ITaxonomyGroup> deliveryResult)
    {
        if (logger is not null)
        {
            LoggerMessages.QueryFailed(logger, "Taxonomy", codename, deliveryResult.StatusCode,
                deliveryResult.Error?.Message, exception: null);
        }
    }

    private void LogQueryCompleted(Stopwatch? stopwatch, HttpStatusCode statusCode, bool cacheHit, bool hasStaleContent = false)
    {
        if (logger is null)
            return;
        stopwatch?.Stop();
        if (hasStaleContent)
            LoggerMessages.QueryStaleContent(logger, codename);
        LoggerMessages.QueryCompleted(logger, "Taxonomy", codename,
            stopwatch?.ElapsedMilliseconds ?? 0, statusCode, cacheHit);
    }
}
