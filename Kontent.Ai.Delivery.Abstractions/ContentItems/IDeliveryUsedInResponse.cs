namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from the Delivery API used in endpoint.
/// </summary>
public interface IDeliveryUsedInResponse
{
    /// <summary>
    /// Gets the items in the response.
    /// </summary>
    IReadOnlyList<IUsedInItem> Items { get; }
}