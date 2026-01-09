using System.Diagnostics;
using System.Net;
using Kontent.Ai.Delivery.Api.Filtering;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.ContentTypes;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITypesQuery"/>
internal sealed class TypesQuery(
    IDeliveryApi api,
    Func<bool?> getDefaultWaitForNewContent,
    IDeliveryCacheManager? cacheManager,
    ILogger? logger = null) : ITypesQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly List<KeyValuePair<string, string>> _serializedFilters = [];
    private ListTypesParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;
    private readonly ILogger? _logger = logger;

    public ITypesQuery WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public ITypesQuery Skip(int skip)
    {
        _params = _params with { Skip = skip };
        return this;
    }

    public ITypesQuery Limit(int limit)
    {
        _params = _params with { Limit = limit };
        return this;
    }

    public ITypesQuery Where(Func<ITypesFilterBuilder, ITypesFilterBuilder> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        build(new TypesFilterBuilder(_serializedFilters));
        return this;
    }

    public ITypesQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IReadOnlyList<IContentType>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Start timing if logging is enabled
        var stopwatch = _logger?.IsEnabled(LogLevel.Information) == true ? Stopwatch.StartNew() : null;

        // Cache check (if enabled)
        string? cacheKey = null;
        if (_cacheManager != null)
        {
            try
            {
                cacheKey = CacheKeyBuilder.BuildTypesKey(_params, _serializedFilters);
                var cached = await _cacheManager.GetAsync<List<ContentType>>(cacheKey, cancellationToken)
                    .ConfigureAwait(false);

                if (cached != null)
                {
                    // Log cache hit
                    if (_logger != null)
                    {
                        LoggerMessages.QueryCacheHit(_logger, cacheKey);
                        LoggerMessages.QueryCompleted(_logger, "Types", "list",
                            stopwatch?.ElapsedMilliseconds ?? 0, HttpStatusCode.OK, cacheHit: true);
                    }
                    var cachedResult = cached.Cast<IContentType>().ToList().AsReadOnly();
                    return DeliveryResult.CacheHit<IReadOnlyList<IContentType>>(cachedResult);
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
        var response = await _api.GetTypesInternalAsync(
            _params,
            FilterQueryParams.ToQueryDictionary(_serializedFilters),
            wait).ConfigureAwait(false);
        var deliveryResult = await response.ToDeliveryResultAsync().ConfigureAwait(false);
        var result = deliveryResult.Map(response => response.Types);

        // Cache result (if enabled) - metadata queries use empty dependencies (rely on TTL for invalidation)
        if (_cacheManager != null && result.IsSuccess && cacheKey != null)
        {
            try
            {
                // Cache the concrete types for proper serialization
                var typesToCache = result.Value.Cast<ContentType>().ToList();
                await _cacheManager.SetAsync(
                    cacheKey,
                    typesToCache,
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
            LoggerMessages.QueryCompleted(_logger, "Types", "list",
                stopwatch?.ElapsedMilliseconds ?? 0, result.StatusCode, cacheHit: false);
        }

        return result;
    }
}