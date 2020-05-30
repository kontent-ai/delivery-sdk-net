using Kentico.Kontent.Delivery.Abstractions.Models.Type.Element;

namespace Kentico.Kontent.Delivery.Abstractions.Responses
{
    /// <summary>
    /// Represents a response from Kentico Kontent Delivery API that contains a content type element.
    /// </summary>
    public interface IDeliveryElementResponse : IResponse
    {
        /// <summary>
        /// Gets the content type element.
        /// </summary>
        IContentElement Element { get; }
    }
}