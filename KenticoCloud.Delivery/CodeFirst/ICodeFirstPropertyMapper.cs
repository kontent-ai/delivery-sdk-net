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
        /// <param name="modelProperty">CLR property to be compared with <paramref name="fieldName"/>.</param>
        /// <param name="fieldName">Name of the field in <paramref name="contentType"/>.</param>
        /// <param name="contentType">Content type containing <paramref name="fieldName"/>.</param>
        /// <returns>TRUE if <sparamref name="modelProperty"/> is a CLR representation of <paramref name="fieldName"/> in <paramref name="contentType"/>.</returns>
        bool IsMatch(PropertyInfo modelProperty, string fieldName, string contentType);
    }
}