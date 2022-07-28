namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Represents a response from Kontent.ai Delivery API that contains a content type.
    /// </summary>
    public interface IDeliveryTypeResponse : IResponse
    {
        /// <summary>
        /// Gets the content type.
        /// </summary>
        IContentType Type { get; }
    }
}