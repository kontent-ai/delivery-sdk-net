using System.Threading;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;
using Kontent.Ai.Delivery.Caching;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITaxonomiesQuery"/>
internal sealed class TaxonomiesQuery(
    IDeliveryApi api,
    Func<bool?> getDefaultWaitForNewContent,
    IDeliveryCacheManager? cacheManager) : ITaxonomiesQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly TaxonomyFilters _filters = new();
    private readonly Dictionary<string, string> _serializedFilters = [];
    private ListTaxonomyGroupsParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;

    public ITaxonomiesQuery Skip(int skip)
    {
        _params = _params with { Skip = skip };
        return this;
    }

    public ITaxonomiesQuery Limit(int limit)
    {
        _params = _params with { Limit = limit };
        return this;
    }

    public ITaxonomiesQuery Where(Func<ITaxonomyFilters, IFilter> filterBuilder)
    {
        var filter = filterBuilder(_filters);
        var (key, value) = filter.ToQueryParameter();
        _serializedFilters.Add(key, value);
        return this;
    }

    public ITaxonomiesQuery Where(IFilter filter)
    {
        var (key, value) = filter.ToQueryParameter();
        _serializedFilters.Add(key, value);
        return this;
    }

    public ITaxonomiesQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IReadOnlyList<ITaxonomyGroup>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Cache check (if enabled)
        string? cacheKey = null;
        if (_cacheManager != null)
        {
            try
            {
                cacheKey = CacheKeyBuilder.BuildTaxonomiesKey(_params, _serializedFilters);
                var cached = await _cacheManager.GetAsync<IDeliveryResult<IReadOnlyList<ITaxonomyGroup>>>(cacheKey, cancellationToken)
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
        var response = await _api.GetTaxonomiesInternalAsync(_params, _serializedFilters, wait).ConfigureAwait(false);
        var deliveryResult = await response.ToDeliveryResultAsync().ConfigureAwait(false);
        var result = deliveryResult.Map(response => response.Taxonomies.AsReadOnly());

        // Cache result (if enabled) - metadata queries use empty dependencies (rely on TTL for invalidation)
        if (_cacheManager != null && result.IsSuccess && cacheKey != null)
        {
            try
            {
                await _cacheManager.SetAsync(
                    cacheKey,
                    result,
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

        return result;
    }
}