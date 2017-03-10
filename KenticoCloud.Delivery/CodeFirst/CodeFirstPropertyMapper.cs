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
        /// <param name="modelProperty">CLR property to be compared with <see cref="fieldName"/>.</param>
        /// <param name="fieldName">Name of the field in <see cref="contentType"/>.</param>
        /// <param name="contentType">Content type containing <see cref="fieldName"/>.</param>
        /// <returns>TRUE if <see cref="modelProperty"/> is a CLR representation of <see cref="fieldName"/> in <see cref="contentType"/>.</returns>
        public bool IsMatch(PropertyInfo modelProperty, string fieldName, string contentType)
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