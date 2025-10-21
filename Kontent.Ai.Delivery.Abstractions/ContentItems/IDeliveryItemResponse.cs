namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from Kontent.ai Delivery API that contains a content item.
/// </summary>
/// <typeparam name="TModel">The type of a content item in the response.</typeparam>
public interface IDeliveryItemResponse<out TModel>
    where TModel : IElementsModel
{
    /// <summary>
    /// Gets the content item.
    /// </summary>
    IContentItem<TModel> Item { get; }
}