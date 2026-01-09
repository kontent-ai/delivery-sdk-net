namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from Kontent.ai Delivery API that contains a list of content types.
/// </summary>
public interface IDeliveryTypeListingResponse : IPageable
{
    /// <summary>
    /// Gets a read-only list of content types.
    /// </summary>
    IReadOnlyList<IContentType> Types { get; }
}