namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents inline image block within rich text
/// </summary>
public interface IInlineImage : IRichTextBlock
{
    /// <summary>
    /// Unique image identifier.
    /// </summary>
    Guid ImageId { get; }
    /// <summary>
    /// Gets the description of the asset.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the URL of the image.
    /// </summary>
    string Url { get; }

    /// <summary>
    /// Gets the height of the image.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets the width of the image.
    /// </summary>
    int Width { get; }
}
