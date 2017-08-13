using System;
using System.Reflection;
using Newtonsoft.Json;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Default provider for mapping Kentico Cloud content item fields to model properties.
    /// </summary>
    public class CodeFirstPropertyMapper : ICodeFirstPropertyMapper
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
            else
            {
                JsonPropertyAttribute propertyAttr = modelProperty.GetCustomAttribute<JsonPropertyAttribute>();
                if (propertyAttr != null)
                {
                    // Try to get the name of the field from the JSON serialization property
                    return fieldName.Equals(propertyAttr.PropertyName, StringComparison.Ordinal);
                }
                else
                {
                    // Default mapping
                    return fieldName.Replace("_", "").Equals(modelProperty.Name, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }
}