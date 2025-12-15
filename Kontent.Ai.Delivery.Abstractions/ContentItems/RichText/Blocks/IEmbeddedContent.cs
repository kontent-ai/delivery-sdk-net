namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents embedded content (component or linked item) within rich text.
/// </summary>
public interface IEmbeddedContent : IRichTextBlock
{
    /// <summary>
    /// Gets the codename of the content type.
    /// </summary>
    string ContentTypeCodename { get; }

    /// <summary>
    /// Gets the codename of the content item.
    /// </summary>
    string Codename { get; }

    /// <summary>
    /// Gets the name of the content item.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets the identifier of the content item.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Retrieves elements of the linked item or component from modular_content
    /// Cast this to your strongly-typed model for simple access to its properties.
    /// </summary>
    /// <remarks>
    /// This property is null if the content item could not be resolved
    /// (e.g., due to depth limits).
    /// </remarks>
    object? Elements { get; }
}

/// <summary>
/// Represents embedded content (component or linked item) within rich text with strongly-typed elements.
/// </summary>
/// <typeparam name="TModel">The model type of the embedded content elements.</typeparam>
/// <remarks>
/// This interface extends <see cref="IEmbeddedContent"/> to provide compile-time type safety
/// for accessing elements of embedded content. Use pattern matching for type-safe access:
/// <code>
/// foreach (var block in richTextContent)
/// {
///     switch (block)
///     {
///         case IEmbeddedContent&lt;Article&gt; article:
///             var title = article.Elements.Title;
///             Console.WriteLine($"Article: {title}");
///             break;
///         case IEmbeddedContent&lt;Coffee&gt; coffee:
///             var name = coffee.Elements.ProductName;
///             Console.WriteLine($"Coffee: {name}");
///             break;
///     }
/// }
/// </code>
/// </remarks>
public interface IEmbeddedContent<out TModel> : IEmbeddedContent // TODO: consider having strongly typed embedded content direct (without nested access required)
{
    /// <summary>
    /// Gets the strongly-typed elements of the embedded content.
    /// </summary>
    new TModel Elements { get; }
}