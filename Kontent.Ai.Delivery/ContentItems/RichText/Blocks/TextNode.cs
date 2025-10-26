namespace Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

/// <inheritdoc cref="ITextNode"/>
internal sealed record TextNode(string Text) : ITextNode;