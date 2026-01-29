namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from Kontent.ai Delivery API that contains a content item.
/// Internal: Used only for JSON deserialization, then mapped to domain models.
/// </summary>
/// <typeparam name="TModel">The type of a content item in the response.</typeparam>
internal interface IDeliveryItemResponse<out TModel>
{
    /// <summary>
    /// Gets the content item.
    /// </summary>
    IContentItem<TModel> Item { get; }
}