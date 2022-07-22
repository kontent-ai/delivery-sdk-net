using System.Collections.Generic;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Represents a response from Kontent.ai Delivery API that contains a content item.
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