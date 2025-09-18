using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using System.Linq;

namespace Kontent.Ai.Delivery.Caching.QueryBuilders;

/// <summary>
/// Caching proxy for multiple taxonomies queries.
/// </summary>
internal sealed class CachingTaxonomiesQuery : ITaxonomiesQuery
{
    private readonly ITaxonomiesQuery _innerQuery;
    private readonly IDeliveryCacheManager _cacheManager;
    private readonly List<string> _queryStateComponents = new();

    public CachingTaxonomiesQuery(
        ITaxonomiesQuery innerQuery,
        IDeliveryCacheManager cacheManager)
    {
        _innerQuery = innerQuery ?? throw new ArgumentNullException(nameof(innerQuery));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        
        // Initialize cache key components
        _queryStateComponents.Add("taxonomies");
    }

    public ITaxonomiesQuery Skip(int count)
    {
        _queryStateComponents.Add($"skip:{count}");
        _innerQuery.Skip(count);
        return this;
    }

    public ITaxonomiesQuery Limit(int count)
    {
        _queryStateComponents.Add($"limit:{count}");
        _innerQuery.Limit(count);
        return this;
    }

    public ITaxonomiesQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _queryStateComponents.Add($"wait:{enabled}");
        _innerQuery.WaitForLoadingNewContent(enabled);
        return this;
    }

    public async Task<IDeliveryResult<IReadOnlyList<ITaxonomyGroup>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey();
        
        return await _cacheManager.GetOrAddAsync(
            cacheKey,
            () => _innerQuery.ExecuteAsync(cancellationToken),
            result => result.IsSuccess,
            GetCacheDependencies
        ).ConfigureAwait(false);
    }

    private string GenerateCacheKey()
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.Append("query:");
        keyBuilder.Append(string.Join("|", _queryStateComponents));
        return keyBuilder.ToString();
    }

    private IEnumerable<string> GetCacheDependencies(IDeliveryResult<IReadOnlyList<ITaxonomyGroup>> result)
    {
        if (result?.IsSuccess == true && result.Value != null)
        {
            return result.Value.SelectMany(taxonomy => CacheHelpers.GetTaxonomyDependencies(taxonomy));
        }
        return Array.Empty<string>();
    }

    public ITaxonomiesQuery Where(Func<ITaxonomyFilters, IFilter> filterBuilder)
    {
        _queryStateComponents.Add($"filter:nocache:{Guid.NewGuid()}");
        _innerQuery.Where(filterBuilder);
        return this;
    }

    public ITaxonomiesQuery Where(IFilter filter)
    {
        _queryStateComponents.Add($"filter:nocache:{Guid.NewGuid()}");
        _innerQuery.Where(filter);
        return this;
    }
}