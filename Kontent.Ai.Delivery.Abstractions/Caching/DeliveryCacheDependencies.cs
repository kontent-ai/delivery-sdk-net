namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Defines standard synthetic dependency keys used by the SDK cache invalidation system.
/// </summary>
public static class DeliveryCacheDependencies
{
    /// <summary>
    /// Synthetic dependency attached to cached item-list query results (for example, <c>GetItems&lt;T&gt;()</c>).
    /// Invalidating this key clears all cached item-list queries for the current cache namespace.
    /// </summary>
    public const string ItemsListScope = "scope_items_list";

    /// <summary>
    /// Synthetic dependency attached to cached content type listing query results (for example, <c>GetTypes()</c>).
    /// Invalidating this key clears all cached content type listing queries for the current cache namespace.
    /// </summary>
    public const string TypesListScope = "scope_types_list";

    /// <summary>
    /// Synthetic dependency attached to cached taxonomy listing query results (for example, <c>GetTaxonomies()</c>).
    /// Invalidating this key clears all cached taxonomy listing queries for the current cache namespace.
    /// </summary>
    public const string TaxonomiesListScope = "scope_taxonomies_list";
}
