using System.Security.Cryptography;
using System.Text;

namespace Kontent.Ai.Delivery.Caching;

/// <summary>
/// Builds stable, deterministic cache keys from query parameters.
/// Keys are order-independent and human-readable for debugging.
/// </summary>
/// <remarks>
/// Cache keys follow the format: {queryType}:{commonParams}:{hash(complexParams)}
///
/// Examples:
/// <list type="bullet">
/// <item>Single Item: <c>item:hero:lang=en-US:depth=2:elements=description|title</c></item>
/// <item>List Items: <c>items:lang=en-US:depth=2:skip=0:limit=10:filters=A7F3E2B9</c></item>
/// <item>Types: <c>types:skip=0:limit=25:elements=codename|name</c></item>
/// <item>Taxonomies: <c>taxonomies:skip=0:limit=100</c></item>
/// </list>
///
/// Keys are:
/// <list type="bullet">
/// <item><b>Deterministic:</b> Same parameters always produce the same key</item>
/// <item><b>Order-independent:</b> Arrays and dictionaries in different orders produce the same key</item>
/// <item><b>Human-readable:</b> Common parameters visible for debugging</item>
/// <item><b>Efficient:</b> Minimal allocations using StringBuilder</item>
/// </list>
/// </remarks>
internal static class CacheKeyBuilder
{
    private const char Separator = ':';
    private const char ArraySeparator = '|';

    /// <summary>
    /// Builds cache key for single item query.
    /// </summary>
    /// <param name="codename">The item codename.</param>
    /// <param name="parameters">The query parameters.</param>
    /// <returns>Cache key in format: <c>item:{codename}:lang={language}:depth={depth}:elements={sorted}</c></returns>
    public static string BuildItemKey(string codename, SingleItemParams parameters)
    {
        var builder = new StringBuilder(128);
        builder.Append("item").Append(Separator);
        builder.Append(codename).Append(Separator);

        AppendLanguage(builder, parameters.Language);
        AppendDepth(builder, parameters.Depth);
        AppendElementProjection(builder, parameters.Elements, parameters.ExcludeElements);

        return TrimTrailingSeparator(builder);
    }

    /// <summary>
    /// Builds cache key for list items query.
    /// </summary>
    /// <param name="parameters">The query parameters.</param>
    /// <param name="filters">The filter dictionary from fluent API.</param>
    /// <returns>Cache key in format: <c>items:lang={language}:depth={depth}:skip={skip}:limit={limit}:filters={hash}</c></returns>
    public static string BuildItemsKey(ListItemsParams parameters, IReadOnlyDictionary<string, string> filters)
    {
        var builder = new StringBuilder(256);
        builder.Append("items").Append(Separator);

        AppendLanguage(builder, parameters.Language);
        AppendDepth(builder, parameters.Depth);
        AppendPagination(builder, parameters.Skip, parameters.Limit);
        AppendOrderBy(builder, parameters.OrderBy);
        AppendIncludeTotalCount(builder, parameters.IncludeTotalCount);
        AppendElementProjection(builder, parameters.Elements, parameters.ExcludeElements);
        AppendFilters(builder, filters);

        return TrimTrailingSeparator(builder);
    }

    /// <summary>
    /// Builds cache key for types query.
    /// </summary>
    /// <param name="parameters">The query parameters.</param>
    /// <param name="filters">The filter dictionary from fluent API.</param>
    /// <returns>Cache key in format: <c>types:skip={skip}:limit={limit}:elements={sorted}:filters={hash}</c></returns>
    public static string BuildTypesKey(ListTypesParams parameters, IReadOnlyDictionary<string, string> filters)
    {
        var builder = new StringBuilder(128);
        builder.Append("types").Append(Separator);

        AppendPagination(builder, parameters.Skip, parameters.Limit);
        AppendElementProjection(builder, parameters.Elements, excludeElements: null);
        AppendFilters(builder, filters);

        return TrimTrailingSeparator(builder);
    }

    /// <summary>
    /// Builds cache key for single type query.
    /// </summary>
    /// <param name="codename">The type codename.</param>
    /// <param name="parameters">The query parameters.</param>
    /// <returns>Cache key in format: <c>type:{codename}:elements={sorted}</c></returns>
    public static string BuildTypeKey(string codename, SingleTypeParams parameters)
    {
        var builder = new StringBuilder(128);
        builder.Append("type").Append(Separator);
        builder.Append(codename).Append(Separator);

        AppendElementProjection(builder, parameters.Elements, excludeElements: null);

        return TrimTrailingSeparator(builder);
    }

    /// <summary>
    /// Builds cache key for taxonomies query.
    /// </summary>
    /// <param name="parameters">The query parameters.</param>
    /// <param name="filters">The filter dictionary from fluent API.</param>
    /// <returns>Cache key in format: <c>taxonomies:skip={skip}:limit={limit}:filters={hash}</c></returns>
    public static string BuildTaxonomiesKey(ListTaxonomyGroupsParams parameters, IReadOnlyDictionary<string, string> filters)
    {
        var builder = new StringBuilder(128);
        builder.Append("taxonomies").Append(Separator);

        AppendPagination(builder, parameters.Skip, parameters.Limit);
        AppendFilters(builder, filters);

        return TrimTrailingSeparator(builder);
    }

    /// <summary>
    /// Builds cache key for single taxonomy query.
    /// </summary>
    /// <param name="codename">The taxonomy group codename.</param>
    /// <returns>Cache key in format: <c>taxonomy:{codename}</c></returns>
    public static string BuildTaxonomyKey(string codename)
    {
        return $"taxonomy{Separator}{codename}";
    }

    // ========== Private Helper Methods ==========

    private static void AppendLanguage(StringBuilder builder, string? language)
    {
        if (!string.IsNullOrEmpty(language))
        {
            builder.Append("lang=").Append(language).Append(Separator);
        }
    }

    private static void AppendDepth(StringBuilder builder, int? depth)
    {
        if (depth.HasValue)
        {
            builder.Append("depth=").Append(depth.Value).Append(Separator);
        }
    }

    private static void AppendPagination(StringBuilder builder, int? skip, int? limit)
    {
        if (skip.HasValue)
        {
            builder.Append("skip=").Append(skip.Value).Append(Separator);
        }
        if (limit.HasValue)
        {
            builder.Append("limit=").Append(limit.Value).Append(Separator);
        }
    }

    private static void AppendOrderBy(StringBuilder builder, string? orderBy)
    {
        if (!string.IsNullOrEmpty(orderBy))
        {
            builder.Append("order=").Append(orderBy).Append(Separator);
        }
    }

    private static void AppendIncludeTotalCount(StringBuilder builder, bool? includeTotalCount)
    {
        if (includeTotalCount == true)
        {
            builder.Append("total").Append(Separator);
        }
    }

    /// <summary>
    /// Appends element projection (Elements or ExcludeElements) in sorted order.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// <list type="bullet">
    /// <item><c>elements=description|title</c></item>
    /// <item><c>exclude=internal_notes|workflow_status</c></item>
    /// </list>
    /// Arrays are sorted to ensure order-independence: ["b", "a"] and ["a", "b"] produce "a|b".
    /// </remarks>
    private static void AppendElementProjection(StringBuilder builder, string[]? elements, string[]? excludeElements)
    {
        if (elements is { Length: > 0 })
        {
            builder.Append("elements=");
            AppendSortedArray(builder, elements);
            builder.Append(Separator);
        }
        else if (excludeElements is { Length: > 0 })
        {
            builder.Append("exclude=");
            AppendSortedArray(builder, excludeElements);
            builder.Append(Separator);
        }
    }

    /// <summary>
    /// Appends sorted array items separated by '|'.
    /// </summary>
    /// <remarks>
    /// Order-independent: ["b", "a"] and ["a", "b"] both produce "a|b".
    /// </remarks>
    private static void AppendSortedArray(StringBuilder builder, string[] items)
    {
        // Sort for determinism
        var sorted = items.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();

        for (int i = 0; i < sorted.Length; i++)
        {
            if (i > 0) builder.Append(ArraySeparator);
            builder.Append(sorted[i]);
        }
    }

    /// <summary>
    /// Appends filters as a stable hash.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Order-independent: {("a", "1"), ("b", "2")} and {("b", "2"), ("a", "1")} produce the same hash.
    /// </para>
    /// <para>
    /// Uses first 8 characters of base64-encoded SHA256 for brevity while maintaining low collision probability
    /// (~1 in 281 trillion for 8 chars).
    /// </para>
    /// <para>
    /// Filters are hashed because they can be very long (e.g., complex queries with many conditions).
    /// </para>
    /// </remarks>
    private static void AppendFilters(StringBuilder builder, IReadOnlyDictionary<string, string> filters)
    {
        if (filters.Count == 0)
            return;

        builder.Append("filters=");

        // Sort by key for determinism
        var sortedFilters = filters.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase);

        // Build stable string representation
        var filterString = string.Join("&",
            sortedFilters.Select(kvp => $"{kvp.Key}={kvp.Value}"));

        // Hash for brevity (filters can be very long)
        var hash = ComputeStableHash(filterString);
        builder.Append(hash);
        builder.Append(Separator);
    }

    /// <summary>
    /// Computes stable, short hash using URL-safe base64 encoding of SHA256.
    /// </summary>
    /// <param name="input">The string to hash.</param>
    /// <returns>An 8-character URL-safe hash providing ~2^48 (281 trillion) possible values.</returns>
    /// <remarks>
    /// <para>
    /// Uses URL-safe base64 encoding (replacing '+' with '-', '/' with '_', and removing '=')
    /// to ensure compatibility with all cache backends. Standard base64 includes characters
    /// that may cause issues with certain cache implementations (e.g., Redis, Memcached).
    /// </para>
    /// <para>
    /// Collision probability is acceptable for cache keys. If collision occurs,
    /// both queries would share the same cache entry (incorrect but not catastrophic).
    /// </para>
    /// </remarks>
    private static string ComputeStableHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);

        // Base64 encode and convert to URL-safe variant
        var base64 = Convert.ToBase64String(hash);

        // Replace standard base64 chars with URL-safe alternatives
        // '+' -> '-', '/' -> '_', remove '=' padding
        var urlSafe = base64
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        // Take first 8 chars (6 bytes of entropy)
        // This gives us 2^48 = 281 trillion possible values
        return urlSafe[..8];
    }

    /// <summary>
    /// Removes trailing separator if present.
    /// </summary>
    private static string TrimTrailingSeparator(StringBuilder builder)
    {
        if (builder.Length > 0 && builder[^1] == Separator)
        {
            builder.Length--;
        }
        return builder.ToString();
    }
}
