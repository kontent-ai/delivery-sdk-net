using System;
using System.Reflection;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentItems.Attributes;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentItems
{
    /// <summary>
    /// Default provider for mapping Kontent content item fields to model properties.
    /// </summary>
    internal class PropertyMapper : IPropertyMapper
    {
        /// <summary>
        /// Determines whether the given property corresponds with a given field.
        /// </summary>
        /// <param name="modelProperty">CLR property to be compared with <paramref name="fieldName"/>.</param>
        /// <param name="fieldName">Name of the field in <paramref name="contentType"/>.</param>
        /// <param name="contentType">Content type containing <paramref name="fieldName"/>.</param>
        /// <returns>TRUE if <paramref name="modelProperty"/> is a CLR representation of <paramref name="fieldName"/> in <paramref name="contentType"/>.</returns>
        public bool IsMatch(PropertyInfo modelProperty, string fieldName, string contentType)
        {
            var ignoreAttribute = modelProperty.GetCustomAttribute<JsonIgnoreAttribute>();
            if (ignoreAttribute != null)
            {
                // If JsonIgnore is set, do not match
                return false;
            }

            var propertyName = GetPropertyNameFromAttribute(modelProperty);
            return propertyName != null
                ? fieldName.Equals(propertyName, StringComparison.Ordinal)
                : fieldName.Replace("_", "").Equals(modelProperty.Name, StringComparison.OrdinalIgnoreCase); // Default mapping
        }

        private static string GetPropertyNameFromAttribute(PropertyInfo modelProperty)
            => modelProperty.GetCustomAttribute<PropertyNameAttribute>()?.PropertyName  // Try to get the name of the property name attribute
            ?? modelProperty.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName; // Try to get the name of JSON serialization property
    }
}