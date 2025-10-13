namespace Kontent.Ai.Delivery.Abstractions.ContentItems.RichText.Blocks;

/// <summary>
/// Represents text content within a rich text HTML node.
/// </summary>
public interface ITextNode : IRichTextBlock
{
    /// <summary>
    /// The actual text content. 
    /// </summary>
    string Text { get; }
}