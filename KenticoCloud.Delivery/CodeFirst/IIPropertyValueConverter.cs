using System.Reflection;
using System;

using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
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
        object GetPropertyValue(PropertyInfo property, JToken elementData, CodeFirstResolvingContext context);
    }
}
