using System.Threading;
using Kontent.Ai.Delivery.Caching;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITaxonomyQuery"/>
internal sealed class TaxonomyQuery(
    IDeliveryApi api,
    string codename,
    Func<bool?> getDefaultWaitForNewContent,
    IDeliveryCacheManager? cacheManager) : ITaxonomyQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;

    public ITaxonomyQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async Task<IDeliveryResult<ITaxonomyGroup>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Cache check (if enabled)
        string? cacheKey = null;
        if (_cacheManager != null)
        {
            try
            {
                cacheKey = CacheKeyBuilder.BuildTaxonomyKey(_codename);
                var cached = await _cacheManager.GetAsync<IDeliveryResult<ITaxonomyGroup>>(cacheKey, cancellationToken)
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
        var response = await _api.GetTaxonomyInternalAsync(_codename, wait).ConfigureAwait(false);
        var deliveryResult = await response.ToDeliveryResultAsync().ConfigureAwait(false);

        // Cache result (if enabled) - metadata queries use empty dependencies (rely on TTL for invalidation)
        if (_cacheManager != null && deliveryResult.IsSuccess && cacheKey != null)
        {
            try
            {
                await _cacheManager.SetAsync(
                    cacheKey,
                    deliveryResult,
                    dependencies: Array.Empty<string>(), // Metadata queries don't track dependencies
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
