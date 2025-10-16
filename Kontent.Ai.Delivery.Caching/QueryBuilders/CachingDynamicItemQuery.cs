using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Caching.QueryBuilders;

/// <summary>
/// Caching proxy for dynamic single item queries.
/// </summary>
internal sealed class CachingDynamicItemQuery : IDynamicItemQuery
{
    private readonly IDynamicItemQuery _innerQuery;
    private readonly IDeliveryCacheManagerLegacy _cacheManager;
    private readonly List<string> _queryStateComponents = new();

    public CachingDynamicItemQuery(
        IDynamicItemQuery innerQuery,
        IDeliveryCacheManagerLegacy cacheManager,
        string codename)
    {
        _innerQuery = innerQuery ?? throw new ArgumentNullException(nameof(innerQuery));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        
        // Initialize cache key components
        _queryStateComponents.Add($"item:{codename}");
        _queryStateComponents.Add("type:dynamic");
    }

    public IDynamicItemQuery WithLanguage(string languageCodename)
    {
        _queryStateComponents.Add($"lang:{languageCodename}");
        _innerQuery.WithLanguage(languageCodename);
        return this;
    }

    public IDynamicItemQuery WithElements(params string[] elementCodenames)
    {
        _queryStateComponents.Add($"elements:{string.Join(",", elementCodenames)}");
        _innerQuery.WithElements(elementCodenames);
        return this;
    }

    public IDynamicItemQuery WithoutElements(params string[] elementCodenames)
    {
        _queryStateComponents.Add($"exclude:{string.Join(",", elementCodenames)}");
        _innerQuery.WithoutElements(elementCodenames);
        return this;
    }

    public IDynamicItemQuery Depth(int depth)
    {
        _queryStateComponents.Add($"depth:{depth}");
        _innerQuery.Depth(depth);
        return this;
    }

    public IDynamicItemQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _queryStateComponents.Add($"wait:{enabled}");
        _innerQuery.WaitForLoadingNewContent(enabled);
        return this;
    }

    public async Task<IDeliveryResult<IContentItem<IElementsModel>>> ExecuteAsync(CancellationToken cancellationToken = default)
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

    private IEnumerable<string> GetCacheDependencies(IDeliveryResult<IContentItem<IElementsModel>> result)
    {
        if (result?.IsSuccess == true && result.Value != null)
        {
            // Extract dependencies from the result
            return CacheHelpers.GetItemDependencies(result.Value);
        }
        return Array.Empty<string>();
    }
}