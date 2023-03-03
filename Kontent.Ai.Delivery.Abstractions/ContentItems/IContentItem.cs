namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents content item.
/// </summary>
public interface IContentItem
{
    /// <summary>
    /// Represents system attributes of a content item.
    /// </summary>
    public IContentItemSystemAttributes System { get; set; }
}
