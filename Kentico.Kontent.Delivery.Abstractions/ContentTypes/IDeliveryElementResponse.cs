using Kentico.Kontent.Delivery.Abstractions.ContentTypes.Element;
using Kentico.Kontent.Delivery.Abstractions.SharedModels;

namespace Kentico.Kontent.Delivery.Abstractions.ContentTypes
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