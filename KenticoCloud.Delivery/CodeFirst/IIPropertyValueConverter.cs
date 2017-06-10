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
        /// <param name="propValue">Source property value</param>
        /// <param name="getContentItem">Callback to retrieve content items by code name</param>
        /// <param name="client">Delivery client</param>
        object GetPropertyValue(PropertyInfo property, JToken propValue, Func<string, object> getContentItem, IDeliveryClient client);
    }
}
