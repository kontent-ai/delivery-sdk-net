using System.Reflection;
using Kentico.Kontent.Delivery.Abstractions.Models.Type.Element;

namespace Kentico.Kontent.Delivery.Abstractions.StrongTyping
{
    /// <summary>
    /// Provides value conversion for the given property
    /// </summary>
    public interface IPropertyValueConverter
    {
        /// <summary>
        /// Gets the property value from property data
        /// </summary>
        /// <param name="property">Property info</param>
        /// <param name="elementData">Source element data</param>
        /// <param name="context">Context of the current resolving process</param>
        object GetPropertyValue(PropertyInfo property, IContentElement elementData, ResolvingContext context);
    }
}
