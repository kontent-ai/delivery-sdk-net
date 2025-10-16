using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using System.Linq;

namespace Kontent.Ai.Delivery.Caching.QueryBuilders;

/// <summary>
/// Caching proxy for multiple types queries.
/// </summary>
internal sealed class CachingTypesQuery : ITypesQuery
{
    private readonly ITypesQuery _innerQuery;
    private readonly IDeliveryCacheManagerLegacy _cacheManager;
    private readonly List<string> _queryStateComponents = new();

    public CachingTypesQuery(
        ITypesQuery innerQuery,
        IDeliveryCacheManagerLegacy cacheManager)
    {
        _innerQuery = innerQuery ?? throw new ArgumentNullException(nameof(innerQuery));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        
        // Initialize cache key components
        _queryStateComponents.Add("types");
    }

    public ITypesQuery Skip(int count)
    {
        _queryStateComponents.Add($"skip:{count}");
        _innerQuery.Skip(count);
        return this;
    }

    public ITypesQuery Limit(int count)
    {
        _queryStateComponents.Add($"limit:{count}");
        _innerQuery.Limit(count);
        return this;
    }

    public ITypesQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _queryStateComponents.Add($"wait:{enabled}");
        _innerQuery.WaitForLoadingNewContent(enabled);
        return this;
    }

    public async Task<IDeliveryResult<IReadOnlyList<IContentType>>> ExecuteAsync(CancellationToken cancellationToken = default)
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

    private IEnumerable<string> GetCacheDependencies(IDeliveryResult<IReadOnlyList<IContentType>> result)
    {
        if (result?.IsSuccess == true && result.Value != null)
        {
            return result.Value.SelectMany(type => CacheHelpers.GetTypeDependencies(type));
        }
        return Array.Empty<string>();
    }

    public ITypesQuery WithElements(params string[] elementCodenames)
    {
        _queryStateComponents.Add($"elements:{string.Join(",", elementCodenames)}");
        _innerQuery.WithElements(elementCodenames);
        return this;
    }

    public ITypesQuery Where(Func<ITypeFilters, IFilter> filterBuilder)
    {
        _queryStateComponents.Add($"filter:nocache:{Guid.NewGuid()}");
        _innerQuery.Where(filterBuilder);
        return this;
    }

    public ITypesQuery Where(IFilter filter)
    {
        _queryStateComponents.Add($"filter:nocache:{Guid.NewGuid()}");
        _innerQuery.Where(filter);
        return this;
    }
}