using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Maps Kontent.ai element codenames to model properties using JsonPropertyName.
/// Honors JsonIgnore and caches match results.
/// </summary>
internal sealed class PropertyMapper : IPropertyMapper
{
    // Cache property matching results to avoid repeated reflection overhead
    // Key: (PropertyInfo, fieldName), Value: bool match result
    private static readonly ConcurrentDictionary<(PropertyInfo, string), bool> _matchCache = new();
    /// <summary>
    /// Determines whether the given property corresponds with a given field.
    /// Uses caching to avoid repeated reflection overhead.
    /// </summary>
    /// <param name="modelProperty">CLR property to be compared with <paramref name="fieldName"/>.</param>
    /// <param name="fieldName">Name of the field in <paramref name="contentType"/>.</param>
    /// <param name="contentType">Content type containing <paramref name="fieldName"/>.</param>
    /// <returns>TRUE if <paramref name="modelProperty"/> is a CLR representation of <paramref name="fieldName"/> in <paramref name="contentType"/>.</returns>
    public bool IsMatch(PropertyInfo modelProperty, string fieldName, string contentType)
    {
        if (modelProperty.DeclaringType is null)
            return false;

        var cacheKey = (modelProperty, fieldName);

        if (_matchCache.TryGetValue(cacheKey, out var cachedResult))
            return cachedResult;

        if (modelProperty.GetCustomAttribute<JsonIgnoreAttribute>() != null)
            return _matchCache[cacheKey] = false;

        var jsonName = modelProperty.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
        var isMatch = jsonName != null && fieldName.Equals(jsonName, StringComparison.Ordinal);
        return _matchCache[cacheKey] = isMatch;
    }
}