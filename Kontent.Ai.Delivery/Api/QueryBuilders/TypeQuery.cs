using System.Diagnostics;
using System.Net;
using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;
using Kontent.Ai.Delivery.Caching;
using Kontent.Ai.Delivery.ContentTypes;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITypeQuery"/>
internal sealed class TypeQuery(
    IDeliveryApi api,
    string codename,
    IDeliveryCacheManager? cacheManager,
    ILogger? logger = null) : ITypeQuery, ICacheExpirationConfigurable
{
    private readonly QueryLoggingHelper _log = new(logger, "Type", codename);
    private SingleTypeParams _params = new();
    private bool _waitForLoadingNewContent;
    public TimeSpan? CacheExpiration { get; set; }

    public ITypeQuery WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = string.Join(",", elementCodenames) };
        return this;
    }

    public ITypeQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContent = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IContentType>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _log.LogQueryStarting();
        var stopwatch = _log.StartTimingIfEnabled();
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

    private async Task<IDeliveryResult<IContentType>> ExecuteWithCacheAsync(
        IDeliveryCacheManager cacheManager,
        Stopwatch? stopwatch,
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildTypeKey(codename, _params);
        IDeliveryResult<IContentType>? apiResult = null;
        var factoryInvoked = false;

        var cached = await cacheManager.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                factoryInvoked = true;
                apiResult = await FetchFromApiAsync(waitForLoadingNewContent, ct).ConfigureAwait(false);
                if (!apiResult.IsSuccess)
                    return null;

                var dependencies = BuildDependencies(apiResult.Value);
                return new CacheEntry<ContentType>((ContentType)apiResult.Value, dependencies);
            },
            CacheExpiration,
            cancellationToken).ConfigureAwait(false);

        // Cache hit (factory never called) or fail-safe served stale data after HTTP error
        if (cached is not null && (apiResult is null || !apiResult.IsSuccess))
        {
            _log.LogQueryCompleted(stopwatch, HttpStatusCode.OK, cacheHit: true);
            var isFailSafe = factoryInvoked
                || (cacheManager is IFailSafeStateProvider failSafeProvider && failSafeProvider.IsFailSafeActive(cacheKey));

            return isFailSafe
                ? DeliveryResult.FailSafeHit<IContentType>(cached.Value, cached.DependencyKeys)
                : DeliveryResult.CacheHit<IContentType>(cached.Value, cached.DependencyKeys);
        }

        apiResult = QueryExecutionResultHelper.EnsureApiResult(apiResult, "Type", codename);

        if (!apiResult.IsSuccess)
            _log.LogQueryFailed(apiResult.StatusCode, apiResult.Error?.Message);

        _log.LogQueryCompleted(stopwatch, apiResult.StatusCode, cacheHit: false, apiResult.HasStaleContent);
        return WrapSuccess(
            cached?.Value ?? apiResult.Value,
            apiResult,
            cached?.DependencyKeys ?? BuildDependencies(apiResult.Value));
    }

    private async Task<IDeliveryResult<IContentType>> ExecuteWithoutCacheAsync(
        Stopwatch? stopwatch,
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var deliveryResult = await FetchFromApiAsync(waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
        if (!deliveryResult.IsSuccess)
            _log.LogQueryFailed(deliveryResult.StatusCode, deliveryResult.Error?.Message);

        _log.LogQueryCompleted(stopwatch, deliveryResult.StatusCode, cacheHit: false, deliveryResult.HasStaleContent);
        return deliveryResult.IsSuccess
            ? WrapSuccess(deliveryResult.Value, deliveryResult, BuildDependencies(deliveryResult.Value))
            : deliveryResult;
    }

    private async Task<IDeliveryResult<IContentType>> FetchFromApiAsync(
        bool? waitForLoadingNewContent,
        CancellationToken cancellationToken)
    {
        var response = await api.GetTypeInternalAsync(codename, _params, waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync(logger).ConfigureAwait(false);
    }

    private static string[] BuildDependencies(IContentType contentType)
    {
        var dependency = CacheDependencyKeyBuilder.BuildTypeDependencyKey(contentType.System.Codename);
        return dependency is null ? [] : [dependency];
    }

    private static IDeliveryResult<IContentType> WrapSuccess(
        IContentType contentType,
        IDeliveryResult<IContentType> apiResult,
        IReadOnlyList<string> dependencyKeys)
        => DeliveryResult.SuccessFrom(contentType, apiResult, dependencyKeys);

}
