namespace Kontent.Ai.Delivery.ContentItems.Attributes;

/// <summary>
/// Instructs the <see cref="PropertyMapper"/> to always map to the property with the specified name.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PropertyNameAttribute"/> class.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class PropertyNameAttribute(string propertyName) : Attribute
{
    /// <summary>
    /// Gets or sets the name of the property.
    /// </summary>
    /// <value>The name of the property.</value>
    public string PropertyName { get; set; } = propertyName;
}

