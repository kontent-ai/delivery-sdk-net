namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from Kontent.ai Delivery API that contains a content type.
/// Internal: Used only for JSON deserialization, then mapped to domain models.
/// </summary>
internal interface IDeliveryTypeResponse
{
    /// <summary>
    /// Gets the content type.
    /// </summary>
    IContentType Type { get; }
}