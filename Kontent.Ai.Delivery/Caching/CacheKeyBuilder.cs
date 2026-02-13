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
    /// <param name="modelType">
    /// Optional model type discriminator used to isolate hydrated-object caches for different generic model types.
    /// </param>
    /// <returns>Cache key in format: <c>item:{codename}:lang={language}:depth={depth}:elements={sorted}</c></returns>
    public static string BuildItemKey(string codename, SingleItemParams parameters, Type? modelType = null)
    {
        var builder = new StringBuilder(128);
        builder.Append("item").Append(Separator);
        builder.Append(codename).Append(Separator);

        AppendModelDiscriminator(builder, modelType);
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
    /// <param name="modelType">
    /// Optional model type discriminator used to isolate hydrated-object caches for different generic model types.
    /// </param>
    /// <returns>Cache key in format: <c>items:lang={language}:depth={depth}:skip={skip}:limit={limit}:filters={hash}</c></returns>
    public static string BuildItemsKey(
        ListItemsParams parameters,
        IReadOnlyList<KeyValuePair<string, string>> filters,
        Type? modelType = null)
    {
        var builder = new StringBuilder(256);
        builder.Append("items").Append(Separator);

        AppendModelDiscriminator(builder, modelType);
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
    public static string BuildTypesKey(ListTypesParams parameters, IReadOnlyList<KeyValuePair<string, string>> filters)
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
    public static string BuildTaxonomiesKey(ListTaxonomyGroupsParams parameters, IReadOnlyList<KeyValuePair<string, string>> filters)
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
        var builder = new StringBuilder(128);
        builder.Append("taxonomy").Append(Separator).Append(codename).Append(Separator);
        return TrimTrailingSeparator(builder);
    }

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

    private static void AppendSortedArray(StringBuilder builder, string[] items)
    {
        var sorted = items.OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
        builder.AppendJoin(ArraySeparator, sorted);
    }

    private static void AppendFilters(StringBuilder builder, IReadOnlyList<KeyValuePair<string, string>> filters)
    {
        if (filters is not { Count: > 0 })
            return;

        var sortedFilters = filters
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(kvp => kvp.Value, StringComparer.Ordinal)
            .Select(kvp => $"{kvp.Key}={kvp.Value}");

        var filterString = string.Join('&', sortedFilters);
        var hash = ComputeStableHash(filterString);

        builder.Append("filters=").Append(hash).Append(Separator);
    }

    private static void AppendModelDiscriminator(StringBuilder builder, Type? modelType)
    {
        if (modelType is null)
            return;

        var identifier = modelType.FullName ?? modelType.Name;
        if (string.IsNullOrWhiteSpace(identifier))
            return;

        builder.Append("model=").Append(ComputeStableHash(identifier)).Append(Separator);
    }

    private static string ComputeStableHash(string input)
    {
        Span<byte> hashBuffer = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(Encoding.UTF8.GetBytes(input), hashBuffer);

        // Take first 9 bytes (9 * 8 / 6 = 12 base64 chars = 72 bits of entropy)
        var base64 = Convert.ToBase64String(hashBuffer[..9]);

        // Replace standard base64 chars with URL-safe alternatives: '+' -> '-', '/' -> '_'
        return base64.Replace('+', '-').Replace('/', '_');
    }

    private static string TrimTrailingSeparator(StringBuilder builder)
    {
        if (builder.Length > 0 && builder[^1] == Separator)
        {
            builder.Length--;
        }
        return builder.ToString();
    }
}
