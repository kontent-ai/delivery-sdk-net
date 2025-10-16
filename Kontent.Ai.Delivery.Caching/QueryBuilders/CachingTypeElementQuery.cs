using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Caching.QueryBuilders;

/// <summary>
/// Caching proxy for type element queries.
/// </summary>
internal sealed class CachingTypeElementQuery : ITypeElementQuery
{
    private readonly ITypeElementQuery _innerQuery;
    private readonly IDeliveryCacheManagerLegacy _cacheManager;
    private readonly List<string> _queryStateComponents = new();

    public CachingTypeElementQuery(
        ITypeElementQuery innerQuery,
        IDeliveryCacheManagerLegacy cacheManager,
        string contentTypeCodename,
        string contentElementCodename)
    {
        _innerQuery = innerQuery ?? throw new ArgumentNullException(nameof(innerQuery));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        
        // Initialize cache key components
        _queryStateComponents.Add($"element:{contentTypeCodename}:{contentElementCodename}");
    }

    public ITypeElementQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _queryStateComponents.Add($"wait:{enabled}");
        _innerQuery.WaitForLoadingNewContent(enabled);
        return this;
    }

    public async Task<IDeliveryResult<IContentElement>> ExecuteAsync(CancellationToken cancellationToken = default)
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

    private IEnumerable<string> GetCacheDependencies(IDeliveryResult<IContentElement> result)
    {
        if (result?.IsSuccess == true && result.Value != null)
        {
            return CacheHelpers.GetContentElementDependencies(result.Value);
        }
        return Array.Empty<string>();
    }
}