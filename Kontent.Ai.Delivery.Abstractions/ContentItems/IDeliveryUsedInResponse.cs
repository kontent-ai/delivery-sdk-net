namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from the Delivery API used in endpoint.
/// Internal: Used only for JSON deserialization, then mapped to domain models.
/// </summary>
internal interface IDeliveryUsedInResponse
{
    /// <summary>
    /// Gets the items in the response.
    /// </summary>
    IReadOnlyList<IUsedInItem> Items { get; }
}
