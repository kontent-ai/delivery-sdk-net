namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a parent content item.
/// </summary>
public interface IUsedInItem : IElementsModel
{
    /// <summary>
    /// Represents system attributes of a parent content item.
    /// </summary>
    public IUsedInItemSystemAttributes System { get; }
}