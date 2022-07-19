using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.ContentItems
{
    /// <inheritdoc cref="IDeliveryItemResponse{T}" />
    internal sealed class DeliveryItemResponse<T> : AbstractItemsResponse, IDeliveryItemResponse<T>
    {
        /// <inheritdoc/>
        public T Item
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemResponse{T}"/> class.
        /// </summary>
        /// <param name="response">The response from Kontent Delivery API that contains a content item.</param>
        /// <param name="item">Content item of a specific type.</param>
        [JsonConstructor]
        internal DeliveryItemResponse(ApiResponse response, T item) : base(response)
        {
            Item = item;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemResponse{T}"/> class.
        /// </summary>
        /// <param name="response">The response from Kontent Delivery API that contains a content item.</param>
        internal DeliveryItemResponse(ApiResponse response) : base(response)
        {
        }
    }
}
