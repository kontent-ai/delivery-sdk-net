using System.Reflection;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Defines the contract for mapping Kentico Cloud content item fields to model properties.
    /// </summary>
    public interface ICodeFirstPropertyMapper
    {
        /// <summary>
        /// Determines whether the given property corresponds with a given field.
        /// </summary>
        /// <param name="modelProperty">CLR property to be compared with <see cref="fieldName"/>.</param>
        /// <param name="fieldName">Name of the field in <see cref="contentType"/>.</param>
        /// <param name="contentType">Content type containing <see cref="fieldName"/>.</param>
        /// <returns>TRUE if <see cref="modelProperty"/> is a CLR representation of <see cref="fieldName"/> in <see cref="contentType"/>.</returns>
        bool IsMatch(PropertyInfo modelProperty, string fieldName, string contentType);
    }
}