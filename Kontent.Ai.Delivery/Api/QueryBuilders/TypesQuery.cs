using System.Threading;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;
using Kontent.Ai.Delivery.Caching;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITypesQuery"/>
internal sealed class TypesQuery(
    IDeliveryApi api,
    Func<bool?> getDefaultWaitForNewContent,
    IDeliveryCacheManager? cacheManager) : ITypesQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly TypeFilters _filters = new();
    private readonly Dictionary<string, string> _serializedFilters = [];
    private ListTypesParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;

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

    public ITypesQuery Where(Func<ITypeFilters, IFilter> filterBuilder)
    {
        var filter = filterBuilder(_filters);
        var (key, value) = filter.ToQueryParameter();
        _serializedFilters.Add(key, value);
        return this;
    }

    public ITypesQuery Where(IFilter filter)
    {
        var (key, value) = filter.ToQueryParameter();
        _serializedFilters.Add(key, value);
        return this;
    }

    public ITypesQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IReadOnlyList<IContentType>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Cache check (if enabled)
        string? cacheKey = null;
        if (_cacheManager != null)
        {
            try
            {
                cacheKey = CacheKeyBuilder.BuildTypesKey(_params, _serializedFilters);
                var cached = await _cacheManager.GetAsync<IDeliveryResult<IReadOnlyList<IContentType>>>(cacheKey, cancellationToken)
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
        var response = await _api.GetTypesInternalAsync(_params, _serializedFilters, wait).ConfigureAwait(false);
        var deliveryResult = await response.ToDeliveryResultAsync().ConfigureAwait(false);
        var result = deliveryResult.Map(response => response.Types.AsReadOnly());

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