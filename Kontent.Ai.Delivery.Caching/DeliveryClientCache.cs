using System;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Caching.QueryBuilders;

namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// Executes requests with cache against the Kontent.ai Delivery API using the new query builder pattern.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DeliveryClientCache"/> class for retrieving cached content of the specified environment.
/// </remarks>
/// <param name="cacheManager">The cache manager for storing and retrieving cached responses.</param>
/// <param name="deliveryClient">The underlying delivery client.</param>
public class DeliveryClientCache(IDeliveryCacheManagerLegacy cacheManager, IDeliveryClient deliveryClient) : IDeliveryClient
{
    private readonly IDeliveryClient _deliveryClient = deliveryClient ?? throw new ArgumentNullException(nameof(deliveryClient));
    private readonly IDeliveryCacheManagerLegacy _deliveryCacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));

    /// <summary>
    /// Returns a query builder for retrieving a single strongly typed content item.
    /// </summary>
    /// <typeparam name="T">Type of the model.</typeparam>
    /// <param name="codename">The codename of a content item.</param>
    /// <returns>A caching query builder that can be configured and executed to retrieve the content item.</returns>
    public IItemQuery<T> GetItem<T>(string codename) where T : IElementsModel
    {
        var innerQuery = _deliveryClient.GetItem<T>(codename);
        return new CachingItemQuery<T>(innerQuery, _deliveryCacheManager, codename);
    }

    /// <summary>
    /// Returns a query builder for retrieving a single content item with runtime/dynamic mapping.
    /// </summary>
    /// <param name="codename">The codename of a content item.</param>
    /// <returns>A caching query builder that can be configured and executed to retrieve the content item.</returns>
    public IDynamicItemQuery GetItem(string codename)
    {
        var innerQuery = _deliveryClient.GetItem(codename);
        return new CachingDynamicItemQuery(innerQuery, _deliveryCacheManager, codename);
    }

    /// <summary>
    /// Returns a query builder for retrieving multiple strongly typed content items.
    /// </summary>
    /// <typeparam name="T">Type of the model.</typeparam>
    /// <returns>A caching query builder that can be configured and executed to retrieve content items.</returns>
    public IItemsQuery<T> GetItems<T>() where T : IElementsModel
    {
        var innerQuery = _deliveryClient.GetItems<T>();
        return new CachingItemsQuery<T>(innerQuery, _deliveryCacheManager);
    }

    /// <summary>
    /// Returns a query builder for retrieving multiple content items with runtime/dynamic mapping.
    /// </summary>
    /// <returns>A caching query builder that can be configured and executed to retrieve content items.</returns>
    public IDynamicItemsQuery GetItems()
    {
        var innerQuery = _deliveryClient.GetItems();
        return new CachingDynamicItemsQuery(innerQuery, _deliveryCacheManager);
    }

    /// <summary>
    /// Returns a query builder for enumerating through strongly typed content items using a feed.
    /// Note: Feed queries are not cached due to their streaming nature.
    /// </summary>
    /// <typeparam name="T">Type of the model.</typeparam>
    /// <returns>A pass-through query builder that directly uses the underlying client.</returns>
    public IEnumerateItemsQuery<T> GetItemsFeed<T>() where T : IElementsModel
    {
        // Feed queries are not cached due to their streaming nature
        return _deliveryClient.GetItemsFeed<T>();
    }

    /// <summary>
    /// Returns a query builder for enumerating through content items using a feed with runtime/dynamic mapping.
    /// Note: Feed queries are not cached due to their streaming nature.
    /// </summary>
    /// <returns>A pass-through query builder that directly uses the underlying client.</returns>
    public IEnumerateItemsQuery<IElementsModel> GetItemsFeed()
    {
        // Feed queries are not cached due to their streaming nature
        return _deliveryClient.GetItemsFeed();
    }

    /// <summary>
    /// Returns a query builder for retrieving a single content type.
    /// </summary>
    /// <param name="codename">The codename of a content type.</param>
    /// <returns>A caching query builder that can be executed to retrieve the content type.</returns>
    public ITypeQuery GetType(string codename)
    {
        var innerQuery = _deliveryClient.GetType(codename);
        return new CachingTypeQuery(innerQuery, _deliveryCacheManager, codename);
    }

    /// <summary>
    /// Returns a query builder for retrieving multiple content types.
    /// </summary>
    /// <returns>A caching query builder that can be configured and executed to retrieve content types.</returns>
    public ITypesQuery GetTypes()
    {
        var innerQuery = _deliveryClient.GetTypes();
        return new CachingTypesQuery(innerQuery, _deliveryCacheManager);
    }

    /// <summary>
    /// Returns a query builder for retrieving a content type element.
    /// </summary>
    /// <param name="contentTypeCodename">The codename of the content type.</param>
    /// <param name="contentElementCodename">The codename of the content type element.</param>
    /// <returns>A caching query builder that can be executed to retrieve the content type element.</returns>
    public ITypeElementQuery GetContentElement(string contentTypeCodename, string contentElementCodename)
    {
        var innerQuery = _deliveryClient.GetContentElement(contentTypeCodename, contentElementCodename);
        return new CachingTypeElementQuery(innerQuery, _deliveryCacheManager, contentTypeCodename, contentElementCodename);
    }

    /// <summary>
    /// Returns a query builder for retrieving a single taxonomy group.
    /// </summary>
    /// <param name="codename">The codename of a taxonomy group.</param>
    /// <returns>A caching query builder that can be executed to retrieve the taxonomy group.</returns>
    public ITaxonomyQuery GetTaxonomy(string codename)
    {
        var innerQuery = _deliveryClient.GetTaxonomy(codename);
        return new CachingTaxonomyQuery(innerQuery, _deliveryCacheManager, codename);
    }

    /// <summary>
    /// Returns a query builder for retrieving multiple taxonomy groups.
    /// </summary>
    /// <returns>A caching query builder that can be configured and executed to retrieve taxonomy groups.</returns>
    public ITaxonomiesQuery GetTaxonomies()
    {
        var innerQuery = _deliveryClient.GetTaxonomies();
        return new CachingTaxonomiesQuery(innerQuery, _deliveryCacheManager);
    }

    /// <summary>
    /// Returns a query builder for retrieving all active languages.
    /// </summary>
    /// <returns>A caching query builder that can be configured and executed to retrieve languages.</returns>
    public ILanguagesQuery GetLanguages()
    {
        var innerQuery = _deliveryClient.GetLanguages();
        return new CachingLanguagesQuery(innerQuery, _deliveryCacheManager);
    }


    /// <summary>
    /// Returns a query builder for retrieving content items that use the specified content item.
    /// Note: Used-in queries are not cached due to their feed-like nature.
    /// </summary>
    /// <param name="codename">The codename of a content item.</param>
    /// <returns>A pass-through query builder that directly uses the underlying client.</returns>
    public IItemUsedInQuery GetItemUsedIn(string codename)
    {
        // Used-in queries are not cached due to their feed-like nature
        return _deliveryClient.GetItemUsedIn(codename);
    }

    /// <summary>
    /// Returns a query builder for retrieving content items that use the specified asset.
    /// Note: Used-in queries are not cached due to their feed-like nature.
    /// </summary>
    /// <param name="codename">The codename of an asset.</param>
    /// <returns>A pass-through query builder that directly uses the underlying client.</returns>
    public IAssetUsedInQuery GetAssetUsedIn(string codename)
    {
        // Used-in queries are not cached due to their feed-like nature
        return _deliveryClient.GetAssetUsedIn(codename);
    }
}
