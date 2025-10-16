using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using System.Linq;

namespace Kontent.Ai.Delivery.Caching.QueryBuilders;

/// <summary>
/// Caching proxy for dynamic multiple items queries.
/// </summary>
internal sealed class CachingDynamicItemsQuery : IDynamicItemsQuery
{
    private readonly IDynamicItemsQuery _innerQuery;
    private readonly IDeliveryCacheManagerLegacy _cacheManager;
    private readonly List<string> _queryStateComponents = new();

    public CachingDynamicItemsQuery(
        IDynamicItemsQuery innerQuery,
        IDeliveryCacheManagerLegacy cacheManager)
    {
        _innerQuery = innerQuery ?? throw new ArgumentNullException(nameof(innerQuery));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        
        // Initialize cache key components
        _queryStateComponents.Add("items");
        _queryStateComponents.Add("type:dynamic");
    }

    public IDynamicItemsQuery WithLanguage(string languageCodename)
    {
        _queryStateComponents.Add($"lang:{languageCodename}");
        _innerQuery.WithLanguage(languageCodename);
        return this;
    }

    public IDynamicItemsQuery WithElements(params string[] elementCodenames)
    {
        _queryStateComponents.Add($"elements:{string.Join(",", elementCodenames)}");
        _innerQuery.WithElements(elementCodenames);
        return this;
    }

    public IDynamicItemsQuery WithoutElements(params string[] elementCodenames)
    {
        _queryStateComponents.Add($"exclude:{string.Join(",", elementCodenames)}");
        _innerQuery.WithoutElements(elementCodenames);
        return this;
    }

    public IDynamicItemsQuery Depth(int depth)
    {
        _queryStateComponents.Add($"depth:{depth}");
        _innerQuery.Depth(depth);
        return this;
    }

    public IDynamicItemsQuery OrderBy(string element, bool sortOrder = true)
    {
        _queryStateComponents.Add($"order:{element}:{sortOrder}");
        _innerQuery.OrderBy(element, sortOrder);
        return this;
    }

    public IDynamicItemsQuery OrderByDescending(string element)
    {
        _queryStateComponents.Add($"order:{element}:desc");
        _innerQuery.OrderBy(element, false);
        return this;
    }

    public IDynamicItemsQuery Skip(int count)
    {
        _queryStateComponents.Add($"skip:{count}");
        _innerQuery.Skip(count);
        return this;
    }

    public IDynamicItemsQuery Limit(int count)
    {
        _queryStateComponents.Add($"limit:{count}");
        _innerQuery.Limit(count);
        return this;
    }

    public IDynamicItemsQuery WithTotalCount()
    {
        _queryStateComponents.Add("totalcount:true");
        _innerQuery.WithTotalCount();
        return this;
    }

    public IDynamicItemsQuery Where(IFilter buildFilter)
    {
        // Disable caching for filtered queries to prevent cache collisions
        // Each filtered query gets a unique cache key to avoid incorrect results
        _queryStateComponents.Add($"filter:nocache:{Guid.NewGuid()}");
        _innerQuery.Where(buildFilter);
        return this;
    }

    public IDynamicItemsQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _queryStateComponents.Add($"wait:{enabled}");
        _innerQuery.WaitForLoadingNewContent(enabled);
        return this;
    }

    public async Task<IDeliveryResult<IReadOnlyList<IContentItem<IElementsModel>>>> ExecuteAsync(CancellationToken cancellationToken = default)
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

    private IEnumerable<string> GetCacheDependencies(IDeliveryResult<IReadOnlyList<IContentItem<IElementsModel>>> result)
    {
        if (result?.IsSuccess == true && result.Value != null)
        {
            // Extract dependencies from all items in the result
            return result.Value.SelectMany(CacheHelpers.GetItemDependencies).Append(CacheHelpers.GetItemsDependencyKey()).Distinct();
        }
        return Array.Empty<string>();
    }

    public IDynamicItemsQuery Filter(Func<IItemFilters, IFilter> filterBuilder)
    {
        _queryStateComponents.Add($"filter:nocache:{Guid.NewGuid()}");
        _innerQuery.Filter(filterBuilder);
        return this;
    }

    public Task<IDeliveryResult<IReadOnlyList<IContentItem<IElementsModel>>>> ExecuteAllAsync(CancellationToken cancellationToken = default)
    {
        return _innerQuery.ExecuteAllAsync(cancellationToken);
    }
}