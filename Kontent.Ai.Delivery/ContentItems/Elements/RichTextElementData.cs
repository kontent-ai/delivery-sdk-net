namespace Kontent.Ai.Delivery.ContentItems.Elements;

/// <summary>
/// Internal record implementing <see cref="IRichTextElementValue"/> for standalone rich text parsing.
/// Used by <see cref="RichTextExtensions.ParseRichTextAsync"/> to wrap raw JSON data.
/// </summary>
internal sealed record RichTextElementData : IRichTextElementValue
{
    /// <inheritdoc />
    public required string Type { get; init; }

    /// <inheritdoc />
    public required string Name { get; init; }

    /// <inheritdoc />
    public required string Codename { get; init; }

    /// <inheritdoc />
    public required string Value { get; init; }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, IInlineImage> Images { get; init; } = new Dictionary<Guid, IInlineImage>();

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, IContentLink> Links { get; init; } = new Dictionary<Guid, IContentLink>();

    /// <inheritdoc />
    public IReadOnlyList<string> ModularContent { get; init; } = [];
}
