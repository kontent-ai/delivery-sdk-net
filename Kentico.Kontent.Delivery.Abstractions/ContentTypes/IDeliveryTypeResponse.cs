using Kentico.Kontent.Delivery.Abstractions.SharedModels;

namespace Kentico.Kontent.Delivery.Abstractions.ContentTypes
{
    /// <summary>
    /// Represents a response from Kontent Delivery API that contains a content type.
    /// </summary>
    public interface IDeliveryTypeResponse : IResponse
    {
        /// <summary>
        /// Gets the content type.
        /// </summary>
        IContentType Type { get; }
    }
}