using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using System.Linq;

namespace Kontent.Ai.Delivery.Caching.QueryBuilders;

/// <summary>
/// Caching proxy for languages queries.
/// </summary>
internal sealed class CachingLanguagesQuery : ILanguagesQuery
{
    private readonly ILanguagesQuery _innerQuery;
    private readonly IDeliveryCacheManagerLegacy _cacheManager;
    private readonly List<string> _queryStateComponents = new();

    public CachingLanguagesQuery(
        ILanguagesQuery innerQuery,
        IDeliveryCacheManagerLegacy cacheManager)
    {
        _innerQuery = innerQuery ?? throw new ArgumentNullException(nameof(innerQuery));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        
        // Initialize cache key components
        _queryStateComponents.Add("languages");
    }

    public ILanguagesQuery Skip(int count)
    {
        _queryStateComponents.Add($"skip:{count}");
        _innerQuery.Skip(count);
        return this;
    }

    public ILanguagesQuery Limit(int count)
    {
        _queryStateComponents.Add($"limit:{count}");
        _innerQuery.Limit(count);
        return this;
    }

    public ILanguagesQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _queryStateComponents.Add($"wait:{enabled}");
        _innerQuery.WaitForLoadingNewContent(enabled);
        return this;
    }

    public async Task<IDeliveryResult<IReadOnlyList<ILanguage>>> ExecuteAsync(CancellationToken cancellationToken = default)
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

    private IEnumerable<string> GetCacheDependencies(IDeliveryResult<IReadOnlyList<ILanguage>> result)
    {
        if (result?.IsSuccess == true && result.Value != null)
        {
            return result.Value.SelectMany(language => CacheHelpers.GetLanguagesDependencies(language));
        }
        return Array.Empty<string>();
    }

    public ILanguagesQuery OrderBy(string elementOrAttributePath, bool ascending = true)
    {
        _queryStateComponents.Add($"order:{elementOrAttributePath}:{ascending}");
        _innerQuery.OrderBy(elementOrAttributePath, ascending);
        return this;
    }
}