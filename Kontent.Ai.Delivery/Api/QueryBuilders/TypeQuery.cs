using Kontent.Ai.Delivery.Caching;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITypeQuery"/>
internal sealed class TypeQuery(
    IDeliveryApi api,
    string codename,
    Func<bool?> getDefaultWaitForNewContent,
    IDeliveryCacheManager? cacheManager) : ITypeQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private SingleTypeParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;

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
        // Cache check (if enabled)
        string? cacheKey = null;
        if (_cacheManager != null)
        {
            try
            {
                cacheKey = CacheKeyBuilder.BuildTypeKey(_codename, _params);
                var cached = await _cacheManager.GetAsync<IDeliveryResult<IContentType>>(cacheKey, cancellationToken)
                    .ConfigureAwait(false);

                if (cached != null)
                {
                    return cached; // Cache hit
                }
            }
            catch (Exception)
            {
                // Cache read failed - continue with API call
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
                    deliveryResult,
                    dependencies: [], // Metadata queries don't track dependencies
                    expiration: null,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Cache write failed - still return result
            }
        }

        return deliveryResult;
    }
}