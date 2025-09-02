namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a content item.
/// </summary>
public interface IContentItem<TElements>
{
    /// <summary>
    /// Represents system attributes of a content item.
    /// </summary>
    public IContentItemSystemAttributes System { get; }

    /// <summary>
    /// Represents the elements of a content item.
    /// </summary>
    public TElements Elements { get; }
}
