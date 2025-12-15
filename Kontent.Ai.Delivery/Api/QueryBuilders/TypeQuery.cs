using System.Diagnostics;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.ContentTypes;
using Kontent.Ai.Delivery.Logging;
using Kontent.Ai.Delivery.SharedModels;
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
        // Start timing if logging is enabled
        var stopwatch = _logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

        // Cache check (if enabled)
        string? cacheKey = null;
        if (_cacheManager != null)
        {
            try
            {
                cacheKey = CacheKeyBuilder.BuildTypeKey(_codename, _params);
                var cached = await _cacheManager.GetAsync<ContentType>(cacheKey, cancellationToken)
                    .ConfigureAwait(false);

                if (cached != null)
                {
                    // Log cache hit
                    if (_logger != null)
                    {
                        LoggerMessages.QueryCacheHit(_logger, cacheKey);
                        LoggerMessages.QueryCompleted(_logger, "Type", _codename,
                            stopwatch?.ElapsedMilliseconds ?? 0, 200, cacheHit: true);
                    }
                    return DeliveryResult.CacheHit<IContentType>(cached);
                }

                // Log cache miss
                if (_logger != null)
                    LoggerMessages.QueryCacheMiss(_logger, cacheKey);
            }
            catch (Exception ex)
            {
                // Cache read failed - continue with API call
                if (_logger != null && cacheKey != null)
                    LoggerMessages.CacheGetFailed(_logger, cacheKey, ex);
            }
        }

        // API call
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var response = await _api.GetTypeInternalAsync(_codename, _params, wait).ConfigureAwait(false);
        var deliveryResult = await response.ToDeliveryResultAsync().ConfigureAwait(false);

        // Cache result (if enabled) - metadata queries use empty dependencies (rely on TTL for invalidation)
        if (_cacheManager != null && deliveryResult.IsSuccess && cacheKey != null)
        {
            try
            {
                await _cacheManager.SetAsync(
                    cacheKey,
                    (ContentType)deliveryResult.Value,
                    dependencies: [], // Metadata queries don't track dependencies
                    expiration: null,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Cache write failed - still return result
                if (_logger != null)
                    LoggerMessages.CacheSetFailed(_logger, cacheKey, ex);
            }
        }

        // Log completion
        if (_logger != null)
        {
            stopwatch?.Stop();
            LoggerMessages.QueryCompleted(_logger, "Type", _codename,
                stopwatch?.ElapsedMilliseconds ?? 0, deliveryResult.StatusCode, cacheHit: false);
        }

        return deliveryResult;
    }
}