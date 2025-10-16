using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Caching.QueryBuilders;

/// <summary>
/// Caching proxy for single item queries.
/// </summary>
/// <typeparam name="TModel">Strongly typed elements model of the content item.</typeparam>
internal sealed class CachingItemQuery<TModel> : IItemQuery<TModel>
    where TModel : IElementsModel
{
    private readonly IItemQuery<TModel> _innerQuery;
    private readonly IDeliveryCacheManagerLegacy _cacheManager;
    private readonly List<string> _queryStateComponents = new();

    public CachingItemQuery(
        IItemQuery<TModel> innerQuery,
        IDeliveryCacheManagerLegacy cacheManager,
        string codename)
    {
        _innerQuery = innerQuery ?? throw new ArgumentNullException(nameof(innerQuery));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        
        // Initialize cache key components
        _queryStateComponents.Add($"item:{codename}");
        _queryStateComponents.Add($"type:{typeof(TModel).FullName}");
    }

    public IItemQuery<TModel> WithLanguage(string languageCodename)
    {
        _queryStateComponents.Add($"lang:{languageCodename}");
        _innerQuery.WithLanguage(languageCodename);
        return this;
    }

    public IItemQuery<TModel> WithElements(params string[] elementCodenames)
    {
        _queryStateComponents.Add($"elements:{string.Join(",", elementCodenames)}");
        _innerQuery.WithElements(elementCodenames);
        return this;
    }

    public IItemQuery<TModel> WithoutElements(params string[] elementCodenames)
    {
        _queryStateComponents.Add($"exclude:{string.Join(",", elementCodenames)}");
        _innerQuery.WithoutElements(elementCodenames);
        return this;
    }

    public IItemQuery<TModel> Depth(int depth)
    {
        _queryStateComponents.Add($"depth:{depth}");
        _innerQuery.Depth(depth);
        return this;
    }

    public IItemQuery<TModel> WaitForLoadingNewContent(bool enabled = true)
    {
        _queryStateComponents.Add($"wait:{enabled}");
        _innerQuery.WaitForLoadingNewContent(enabled);
        return this;
    }

    public async Task<IDeliveryResult<IContentItem<TModel>>> ExecuteAsync(CancellationToken cancellationToken = default)
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

    private IEnumerable<string> GetCacheDependencies(IDeliveryResult<IContentItem<TModel>> result)
    {
        if (result?.IsSuccess == true && result.Value != null)
        {
            // Extract dependencies from the result
            return CacheHelpers.GetItemDependencies(result.Value);
        }
        return Array.Empty<string>();
    }
}