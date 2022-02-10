using System;

namespace Kentico.Kontent.Delivery.ContentItems
{
    /// <summary>
    /// Instructs the <see cref="PropertyMapper"/> to always map to the property with the specified name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PropertyNameAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the property.
        /// </summary>
        /// <value>The name of the property.</value>
        public string PropertyName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyNameAttribute"/> class.
        /// </summary>
        public PropertyNameAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
}
