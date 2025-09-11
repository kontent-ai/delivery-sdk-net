namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Defines members necessary for retrieving content and its metadata from the Kontent.ai Delivery service.
/// All methods return query builders that must be executed with ExecuteAsync() to retrieve the actual API response.
/// </summary>
public interface IDeliveryClient
{
    /// <summary>
    /// Returns a query builder for retrieving a single strongly typed content item.
    /// </summary>
    /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
    /// <param name="codename">The codename of a content item.</param>
    /// <returns>A query builder that can be configured and executed to retrieve the content item.</returns>
    IItemQuery<T> GetItem<T>(string codename) where T : IElementsModel;

    /// <summary>
    /// Returns a query builder for retrieving a single content item with runtime/dynamic mapping.
    /// </summary>
    /// <param name="codename">The codename of a content item.</param>
    /// <returns>A query builder that can be configured and executed to retrieve the content item.</returns>
    IDynamicItemQuery GetItem(string codename);

    /// <summary>
    /// Returns a query builder for retrieving multiple strongly typed content items.
    /// </summary>
    /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
    /// <returns>A query builder that can be configured and executed to retrieve content items.</returns>
    IItemsQuery<T> GetItems<T>() where T : IElementsModel;

    /// <summary>
    /// Returns a query builder for retrieving multiple content items with runtime/dynamic mapping.
    /// </summary>
    /// <returns>A query builder that can be configured and executed to retrieve content items.</returns>
    IDynamicItemsQuery GetItems();

    /// <summary>
    /// Returns a query builder for enumerating through strongly typed content items using a feed.
    /// </summary>
    /// <typeparam name="T">Type of the model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
    /// <returns>A query builder that can be configured and executed to enumerate through content items.</returns>
    IEnumerateItemsQuery<T> GetItemsFeed<T>() where T : IElementsModel;

    /// <summary>
    /// Returns a query builder for enumerating through content items using a feed with runtime/dynamic mapping.
    /// </summary>
    /// <returns>A query builder that can be configured and executed to enumerate through content items.</returns>
    IEnumerateItemsQuery<IElementsModel> GetItemsFeed();

    /// <summary>
    /// Returns a query builder for retrieving a single content type.
    /// </summary>
    /// <param name="codename">The codename of a content type.</param>
    /// <returns>A query builder that can be configured and executed to retrieve the content type.</returns>
    ITypeQuery GetType(string codename);

    /// <summary>
    /// Returns a query builder for retrieving multiple content types.
    /// </summary>
    /// <returns>A query builder that can be configured and executed to retrieve content types.</returns>
    ITypesQuery GetTypes();

    /// <summary>
    /// Returns a query builder for retrieving a content type element.
    /// </summary>
    /// <param name="contentTypeCodename">The codename of the content type.</param>
    /// <param name="contentElementCodename">The codename of the content type element.</param>
    /// <returns>A query builder that can be executed to retrieve the content type element.</returns>
    ITypeElementQuery GetContentElement(string contentTypeCodename, string contentElementCodename);

    /// <summary>
    /// Returns a query builder for retrieving a single taxonomy group.
    /// </summary>
    /// <param name="codename">The codename of a taxonomy group.</param>
    /// <returns>A query builder that can be executed to retrieve the taxonomy group.</returns>
    ITaxonomyQuery GetTaxonomy(string codename);

    /// <summary>
    /// Returns a query builder for retrieving multiple taxonomy groups.
    /// </summary>
    /// <returns>A query builder that can be configured and executed to retrieve taxonomy groups.</returns>
    ITaxonomiesQuery GetTaxonomies();

    /// <summary>
    /// Returns a query builder for retrieving all active languages.
    /// </summary>
    /// <returns>A query builder that can be configured and executed to retrieve languages.</returns>
    ILanguagesQuery GetLanguages();

    /// <summary>
    /// Returns a query builder for retrieving content items that use the specified content item.
    /// </summary>
    /// <param name="codename">The codename of a content item.</param>
    /// <returns>A query builder that can be executed to retrieve parent content items.</returns>
    IItemUsedInQuery GetItemUsedIn(string codename);

    /// <summary>
    /// Returns a query builder for retrieving content items that use the specified asset.
    /// </summary>
    /// <param name="codename">The codename of an asset.</param>
    /// <returns>A query builder that can be executed to retrieve parent content items.</returns>
    IAssetUsedInQuery GetAssetUsedIn(string codename);
}