using System.Collections.Concurrent;
using System.Reflection;
using Kontent.Ai.Delivery.ContentItems.Attributes;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Default provider for mapping Kontent.ai content item fields to model properties.
/// Caches reflection results for optimal performance.
/// </summary>
internal class PropertyMapper : IPropertyMapper // TODO: this is just a boolean checker now, consider renaming?
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

        // Check cache first for O(1) lookup
        if (_matchCache.TryGetValue(cacheKey, out var cachedResult))
        {
            return cachedResult;
        }

        // Cache miss - perform matching logic
        var ignoreAttribute = modelProperty.GetCustomAttribute<JsonIgnoreAttribute>();
        if (ignoreAttribute != null)
        {
            // JsonIgnore means no match - cache null to avoid repeated checks
            _matchCache[cacheKey] = false;
            return false;
        }

        var propertyName = GetPropertyNameFromAttribute(modelProperty);
        var isMatch = propertyName != null
            ? fieldName.Equals(propertyName, StringComparison.Ordinal)
            : fieldName.Replace("_", "").Equals(modelProperty.Name, StringComparison.OrdinalIgnoreCase); // Default mapping

        // Cache the result to avoid repeated reflection
        _matchCache[cacheKey] = isMatch;

        return isMatch;
    }

    private static string? GetPropertyNameFromAttribute(PropertyInfo modelProperty)
        => modelProperty.GetCustomAttribute<PropertyNameAttribute>()?.PropertyName  // Try to get the name of the property name attribute
        ?? modelProperty.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name; // Try to get the name of JSON serialization property
}