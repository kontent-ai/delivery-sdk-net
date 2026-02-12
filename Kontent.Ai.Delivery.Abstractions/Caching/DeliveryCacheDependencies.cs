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
}
