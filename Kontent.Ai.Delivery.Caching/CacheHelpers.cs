using System;
using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// A helper for creating cache dependency keys
/// </summary>
public static class CacheHelpers
{
    #region Constants

    private const string DEPENDENCY_ITEM = "dependency_item";
    private const string DEPENDENCY_ITEM_LISTING = "dependency_item_listing";
    private const string DEPENDENCY_TYPE_LISTING = "dependency_type_listing";
    private const string DEPENDENCY_TAXONOMY_GROUP = "dependency_taxonomy_group";
    private const string DEPENDENCY_TAXONOMY_GROUP_LISTING = "dependency_taxonomy_group_listing";
    private const string DEPENDENCY_LANGUAGE_LISTING = "dependency_language_listing";

    #endregion


    #region Dependency keys

    /// <summary>
    /// Gets a Item dependency key from codeName
    /// </summary>
    /// <returns>Dependency key</returns>
    public static string GetItemDependencyKey(string codename)
    {
        return StringHelpers.Join(DEPENDENCY_ITEM, codename);
    }

    /// <summary>
    /// Gets a Items dependency key
    /// </summary>
    /// <returns>Dependency key</returns>
    public static string GetItemsDependencyKey()
    {
        return DEPENDENCY_ITEM_LISTING;
    }

    /// <summary>
    /// Gets a Types dependency key
    /// </summary>
    /// <returns>Dependency key</returns>
    public static string GetTypesDependencyKey()
    {
        return DEPENDENCY_TYPE_LISTING;
    }

    /// <summary>
    /// Gets a Taxonomy dependency key from codeName
    /// </summary>
    /// <returns>Dependency key</returns>

    public static string GetTaxonomyDependencyKey(string codename)
    {
        return StringHelpers.Join(DEPENDENCY_TAXONOMY_GROUP, codename);
    }

    /// <summary>
    /// Gets Taxonomies dependency key
    /// </summary>
    /// <returns>Dependency key</returns>
    public static string GetTaxonomiesDependencyKey()
    {
        return DEPENDENCY_TAXONOMY_GROUP_LISTING;
    }

    /// <summary>
    /// Gets Languages dependency key
    /// </summary>
    /// <returns>Dependency key</returns>
    public static string GetLanguagesDependencyKey()
    {
        return DEPENDENCY_LANGUAGE_LISTING;
    }

    #endregion

    #region Dependencies


    /// <summary>
    /// Gets item dependencies from a content item (new architecture).
    /// </summary>
    /// <typeparam name="T">The content item type.</typeparam>
    /// <param name="contentItem">The content item.</param>
    /// <returns>Dependency keys.</returns>
    public static IEnumerable<string> GetItemDependencies<T>(IContentItem<T> contentItem) where T : IElementsModel
    {
        if (contentItem?.System?.Codename == null) return Array.Empty<string>();
        return new[] { GetItemDependencyKey(contentItem.System.Codename) };
    }

    /// <summary>
    /// Gets item dependencies from a dynamic content item (new architecture).
    /// </summary>
    /// <param name="contentItem">The content item.</param>
    /// <returns>Dependency keys.</returns>
    public static IEnumerable<string> GetItemDependencies(IContentItem<IElementsModel> contentItem)
    {
        if (contentItem?.System?.Codename == null) return Array.Empty<string>();
        return new[] { GetItemDependencyKey(contentItem.System.Codename) };
    }

    /// <summary>
    /// Gets content type dependencies from a content type (new architecture).
    /// </summary>
    /// <param name="contentType">The content type.</param>
    /// <returns>Dependency keys.</returns>
    public static IEnumerable<string> GetTypeDependencies(IContentType contentType)
    {
        if (contentType?.System?.Codename == null) return Array.Empty<string>();
        return new[] { GetTypesDependencyKey() };
    }

    /// <summary>
    /// Gets content element dependencies from a content element (new architecture).
    /// </summary>
    /// <param name="contentElement">The content element.</param>
    /// <returns>Dependency keys.</returns>
    public static IEnumerable<string> GetContentElementDependencies(IContentElement contentElement)
    {
        if (contentElement?.Codename == null) return Array.Empty<string>();
        return new[] { GetTypesDependencyKey() };
    }

    /// <summary>
    /// Gets taxonomy dependencies from a taxonomy group (new architecture).
    /// </summary>
    /// <param name="taxonomyGroup">The taxonomy group.</param>
    /// <returns>Dependency keys.</returns>
    public static IEnumerable<string> GetTaxonomyDependencies(ITaxonomyGroup taxonomyGroup)
    {
        if (taxonomyGroup?.System?.Codename == null) return Array.Empty<string>();
        return new[] { GetTaxonomyDependencyKey(taxonomyGroup.System.Codename) };
    }

    /// <summary>
    /// Gets language dependencies from a language (new architecture).
    /// </summary>
    /// <param name="language">The language.</param>
    /// <returns>Dependency keys.</returns>
    public static IEnumerable<string> GetLanguagesDependencies(ILanguage language)
    {
        if (language?.System?.Codename == null) return Array.Empty<string>();
        return new[] { GetLanguagesDependencyKey() };
    }

    #endregion
}
