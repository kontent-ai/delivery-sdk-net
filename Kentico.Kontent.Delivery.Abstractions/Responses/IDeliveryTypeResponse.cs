using Kentico.Kontent.Delivery.Abstractions.Models.Type;

namespace Kentico.Kontent.Delivery.Abstractions.Responses
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