namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a response from Kentico Kontent Delivery API that contains a content type.
    /// </summary>
    public interface IDeliveryTypeResponse : IResponse
    {
        /// <summary>
        /// Gets the content type.
        /// </summary>
        IContentType Type { get; }
    }
}