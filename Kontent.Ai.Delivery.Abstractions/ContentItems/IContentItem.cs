namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a content item with system attributes and elements.
/// </summary>
public interface IContentItem
{
    /// <summary>
    /// Gets the system attributes of the content item.
    /// </summary>
    IContentItemSystemAttributes System { get; }

    /// <summary>
    /// Gets the elements of the content item (untyped).
    /// </summary>
    object? Elements { get; }
}

/// <summary>
/// Represents a content item with strongly-typed elements.
/// </summary>
/// <typeparam name="TModel">Strongly typed elements of a content item.</typeparam>
public interface IContentItem<out TModel> : IContentItem
{
    /// <summary>
    /// Gets the strongly-typed elements of the content item.
    /// </summary>
    new TModel Elements { get; }
}