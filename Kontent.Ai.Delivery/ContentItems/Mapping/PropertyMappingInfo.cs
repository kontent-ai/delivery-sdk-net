using System.Linq.Expressions;
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
    /// The PropertyInfo for type and metadata inspection.
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
    /// Compiled setter delegate for fast property assignment.
    /// </summary>
    public required Action<object, object> Setter { get; init; }

    /// <summary>
    /// Precomputed mapping strategy for this property.
    /// </summary>
    public required ElementMappingKind MapKind { get; init; }

    /// <summary>
    /// Sets the property value on the target object using the compiled setter.
    /// </summary>
    public void SetValue(object target, object value) => Setter(target, value);

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
            .Select(x =>
            {
                var propertyType = x.Property.PropertyType;
                var enumerableElementType = TryGetEnumerableElementType(propertyType);
                return new PropertyMappingInfo
                {
                    ElementCodename = x.Codename!,
                    Property = x.Property,
                    PropertyType = propertyType,
                    EnumerableElementType = enumerableElementType,
                    MapKind = DetermineMapKind(propertyType, enumerableElementType),
                    Setter = BuildSetter(x.Property)
                };
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

    private static ElementMappingKind DetermineMapKind(Type propertyType, Type? enumerableElementType)
    {
        if (typeof(IRichTextContent).IsAssignableFrom(propertyType))
        {
            return ElementMappingKind.RichText;
        }

        if (enumerableElementType is not null &&
            (typeof(IAsset).IsAssignableFrom(enumerableElementType) ||
             enumerableElementType == typeof(Asset)))
        {
            return ElementMappingKind.Assets;
        }

        if (enumerableElementType is not null &&
            typeof(ITaxonomyTerm).IsAssignableFrom(enumerableElementType))
        {
            return ElementMappingKind.Taxonomy;
        }

        if (typeof(IDateTimeContent).IsAssignableFrom(propertyType))
        {
            return ElementMappingKind.DateTime;
        }

        if (enumerableElementType is not null &&
            typeof(IEmbeddedContent).IsAssignableFrom(enumerableElementType))
        {
            return ElementMappingKind.LinkedItems;
        }

        return ElementMappingKind.Simple;
    }

    /// <summary>
    /// Builds a compiled setter delegate for faster property assignment.
    /// </summary>
    private static Action<object, object> BuildSetter(PropertyInfo property)
    {
        var targetParam = Expression.Parameter(typeof(object), "target");
        var valueParam = Expression.Parameter(typeof(object), "value");

        // (TTarget)target
        var castTarget = Expression.Convert(targetParam, property.DeclaringType!);

        // (TValue)value (handles both reference types and value type unboxing)
        var castValue = Expression.Convert(valueParam, property.PropertyType);

        // ((TTarget)target).Property = (TValue)value
        var propertyAccess = Expression.Property(castTarget, property);
        var assign = Expression.Assign(propertyAccess, castValue);

        return Expression.Lambda<Action<object, object>>(assign, targetParam, valueParam).Compile();
    }
}
