namespace Kontent.Ai.Delivery.ContentItems.Attributes;

/// <summary>
/// Instructs the <see cref="PropertyMapper"/> to always map to the property with the specified name.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ElementCodenameAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets or sets the name of the element.
    /// </summary>
    /// <value>The name of the element.</value>
    public string Name { get; set; } = name;
}

