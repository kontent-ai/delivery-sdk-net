using System.Globalization;
using Kontent.Ai.Delivery.Api.QueryBuilders.Helpers;

namespace Kontent.Ai.Delivery;

/// <summary>
/// Extension methods for overriding cache expiration per query.
/// </summary>
public static class QueryCacheExtensions
{
    /// <summary>
    /// Overrides cache expiration for the current single-item query.
    /// </summary>
    /// <param name="query">The query builder instance.</param>
    /// <param name="expiration">
    /// Absolute expiration for this query result. Pass <c>null</c> to use the cache manager default.
    /// </param>
    /// <typeparam name="TModel">The content item model type.</typeparam>
    /// <returns>The same query instance for fluent chaining.</returns>
    public static IItemQuery<TModel> WithCacheExpiration<TModel>(this IItemQuery<TModel> query, TimeSpan? expiration)
        => SetCacheExpiration(query, expiration, "GetItem<T>()");

    /// <summary>
    /// Overrides cache expiration for the current item-list query.
    /// </summary>
    /// <param name="query">The query builder instance.</param>
    /// <param name="expiration">
    /// Absolute expiration for this query result. Pass <c>null</c> to use the cache manager default.
    /// </param>
    /// <typeparam name="TModel">The content item model type.</typeparam>
    /// <returns>The same query instance for fluent chaining.</returns>
    public static IItemsQuery<TModel> WithCacheExpiration<TModel>(this IItemsQuery<TModel> query, TimeSpan? expiration)
        => SetCacheExpiration(query, expiration, "GetItems<T>()");

    /// <summary>
    /// Overrides cache expiration for the current single-type query.
    /// </summary>
    /// <param name="query">The query builder instance.</param>
    /// <param name="expiration">
    /// Absolute expiration for this query result. Pass <c>null</c> to use the cache manager default.
    /// </param>
    /// <returns>The same query instance for fluent chaining.</returns>
    public static ITypeQuery WithCacheExpiration(this ITypeQuery query, TimeSpan? expiration)
        => SetCacheExpiration(query, expiration, "GetType()");

    /// <summary>
    /// Overrides cache expiration for the current type-list query.
    /// </summary>
    /// <param name="query">The query builder instance.</param>
    /// <param name="expiration">
    /// Absolute expiration for this query result. Pass <c>null</c> to use the cache manager default.
    /// </param>
    /// <returns>The same query instance for fluent chaining.</returns>
    public static ITypesQuery WithCacheExpiration(this ITypesQuery query, TimeSpan? expiration)
        => SetCacheExpiration(query, expiration, "GetTypes()");

    /// <summary>
    /// Overrides cache expiration for the current single-taxonomy query.
    /// </summary>
    /// <param name="query">The query builder instance.</param>
    /// <param name="expiration">
    /// Absolute expiration for this query result. Pass <c>null</c> to use the cache manager default.
    /// </param>
    /// <returns>The same query instance for fluent chaining.</returns>
    public static ITaxonomyQuery WithCacheExpiration(this ITaxonomyQuery query, TimeSpan? expiration)
        => SetCacheExpiration(query, expiration, "GetTaxonomy()");

    /// <summary>
    /// Overrides cache expiration for the current taxonomy-list query.
    /// </summary>
    /// <param name="query">The query builder instance.</param>
    /// <param name="expiration">
    /// Absolute expiration for this query result. Pass <c>null</c> to use the cache manager default.
    /// </param>
    /// <returns>The same query instance for fluent chaining.</returns>
    public static ITaxonomiesQuery WithCacheExpiration(this ITaxonomiesQuery query, TimeSpan? expiration)
        => SetCacheExpiration(query, expiration, "GetTaxonomies()");

    private static TQuery SetCacheExpiration<TQuery>(TQuery query, TimeSpan? expiration, string queryKind)
        where TQuery : class
    {
        ArgumentNullException.ThrowIfNull(query);

        if (expiration is { } ttl && ttl <= TimeSpan.Zero)
        {
            var suppliedValue = ttl.ToString("c", CultureInfo.InvariantCulture);
            throw new ArgumentOutOfRangeException(
                nameof(expiration),
                ttl,
                $"Cache expiration for {queryKind} must be greater than TimeSpan.Zero. " +
                $"Supplied value: '{suppliedValue}'. Use null to use the cache manager default.");
        }

        if (query is not ICacheExpirationConfigurable configurable)
        {
            throw new NotSupportedException($"{queryKind} does not support cache expiration overrides.");
        }

        configurable.CacheExpiration = expiration;
        return query;
    }
}
