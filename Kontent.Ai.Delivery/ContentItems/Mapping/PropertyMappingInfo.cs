using System.Reflection;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems.Mapping;

/// <summary>
/// Cached property metadata for efficient element-to-property mapping.
/// </summary>
internal sealed class PropertyMappingInfo
{
    /// <summary>
    /// The JSON element codename this property maps to (from JsonPropertyName attribute).
    /// </summary>
    public required string ElementCodename { get; init; }

    /// <summary>
    /// The PropertyInfo for reflection-based value setting.
    /// </summary>
    public required PropertyInfo Property { get; init; }

    /// <summary>
    /// The property type for type-specific mapping decisions.
    /// </summary>
    public required Type PropertyType { get; init; }

    /// <summary>
    /// For IEnumerable&lt;T&gt; properties, the element type T; otherwise null.
    /// </summary>
    public Type? EnumerableElementType { get; init; }

    /// <summary>
    /// Sets the property value on the target object.
    /// </summary>
    public void SetValue(object target, object? value) => Property.SetValue(target, value);

    /// <summary>
    /// Creates property mappings for all writable properties with JsonPropertyName attribute.
    /// </summary>
    public static PropertyMappingInfo[] CreateMappings(Type modelType)
    {
        return modelType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanWrite)
            .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            .Select(p => new { Property = p, Codename = p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name })
            .Where(x => x.Codename is not null)
            .Select(x => new PropertyMappingInfo
            {
                ElementCodename = x.Codename!,
                Property = x.Property,
                PropertyType = x.Property.PropertyType,
                EnumerableElementType = TryGetEnumerableElementType(x.Property.PropertyType)
            })
            .ToArray();
    }

    /// <summary>
    /// Gets the element type for IEnumerable&lt;T&gt; types.
    /// </summary>
    private static Type? TryGetEnumerableElementType(Type type)
    {
        var enumerableInterface = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerableInterface?.GetGenericArguments().FirstOrDefault();
    }
}
