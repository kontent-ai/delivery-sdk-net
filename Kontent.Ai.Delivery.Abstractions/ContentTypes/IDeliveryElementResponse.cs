namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Represents a response from Kontent.ai Delivery API that contains a content type element.
    /// </summary>
    public interface IDeliveryElementResponse : IResponse
    {
        /// <summary>
        /// Gets the content type element.
        /// </summary>
        IContentElement Element { get; }
    }
}