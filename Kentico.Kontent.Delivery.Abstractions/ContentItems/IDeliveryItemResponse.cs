using Kentico.Kontent.Delivery.Abstractions.SharedModels;

namespace Kentico.Kontent.Delivery.Abstractions.ContentItems
{
    /// <summary>
    /// Represents a response from Kontent Delivery API that contains a content item.
    /// </summary>
    /// <typeparam name="T">The type of a content item in the response.</typeparam>
    public interface IDeliveryItemResponse<out T> : IResponse
    {
        /// <summary>
        /// Gets the content item.
        /// </summary>
        T Item { get; }
    }
}