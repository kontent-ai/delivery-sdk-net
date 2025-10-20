namespace Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

/// <inheritdoc cref="ITextNode"/>
internal sealed class TextNode : ITextNode
{
    /// <inheritdoc cref="ITextNode.Text"/>
    public string Text { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextNode"/> class with the specified text content.
    /// </summary>
    public TextNode(string text)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }
}