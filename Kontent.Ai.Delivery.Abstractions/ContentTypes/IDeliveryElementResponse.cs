namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from Kontent.ai Delivery API that contains a content type element.
/// Internal: Used only for JSON deserialization, then mapped to domain models.
/// </summary>
internal interface IDeliveryElementResponse
{
    /// <summary>
    /// Gets the content type element.
    /// </summary>
    IContentElement Element { get; }
}