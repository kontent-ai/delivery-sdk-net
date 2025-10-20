using System;

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
    /// Gets the resolved content item object.
    /// Cast this to your strongly-typed model to access its properties.
    /// </summary>
    /// <remarks>
    /// This property is null if the content item could not be resolved
    /// (e.g., due to depth limits or missing modular content).
    /// </remarks>
    object? Content { get; }
}
