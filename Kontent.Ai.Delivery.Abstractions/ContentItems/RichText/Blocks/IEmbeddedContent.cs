namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents embedded content (component or linked item) within rich text.
/// This is a marker interface that combines <see cref="IContentItem"/> with <see cref="IRichTextBlock"/>
/// to allow content items to appear in rich text blocks.
/// </summary>
/// <remarks>
/// Access content item properties via the <see cref="IContentItem.System"/> property:
/// <code>
/// var type = embeddedContent.System.Type;
/// var codename = embeddedContent.System.Codename;
/// var id = embeddedContent.System.Id;
/// </code>
/// </remarks>
public interface IEmbeddedContent : IContentItem, IRichTextBlock;

/// <summary>
/// Represents embedded content (component or linked item) within rich text with strongly-typed elements.
/// </summary>
/// <typeparam name="TModel">The model type of the embedded content elements.</typeparam>
/// <remarks>
/// This interface extends <see cref="IEmbeddedContent"/> and <see cref="IContentItem{TModel}"/>
/// to provide compile-time type safety for accessing elements of embedded content.
/// Use pattern matching for type-safe access:
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
public interface IEmbeddedContent<out TModel> : IEmbeddedContent, IContentItem<TModel>;
