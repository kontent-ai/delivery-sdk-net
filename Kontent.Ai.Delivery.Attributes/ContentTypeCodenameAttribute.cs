namespace Kontent.Ai.Delivery.Attributes;

/// <summary>
/// Marks a class as a content type model and specifies its Kontent.ai codename.
/// Used by the source generator to create a compile-time type registry.
/// </summary>
/// <remarks>
/// Apply this attribute to content type model classes to enable automatic
/// type resolution without manual <c>ITypeProvider</c> implementation.
/// </remarks>
/// <example>
/// <code>
/// [ContentTypeCodename("article")]
/// public record Article
/// {
///     // ...
/// }
/// </code>
/// </example>
/// <remarks>
/// Initializes a new instance of the <see cref="ContentTypeCodenameAttribute"/> class.
/// </remarks>
/// <param name="codename">The content type codename as defined in Kontent.ai.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class ContentTypeCodenameAttribute(string codename) : Attribute
{
    /// <summary>
    /// Gets the content type codename as defined in Kontent.ai.
    /// </summary>
    public string Codename { get; } = codename;
}
