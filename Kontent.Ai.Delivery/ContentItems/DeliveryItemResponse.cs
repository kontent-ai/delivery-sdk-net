using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems
{
    /// <inheritdoc cref="IDeliveryItemResponse{T}" />
    internal sealed class DeliveryItemResponse<T> : IDeliveryItemResponse<T>
    {
        /// <inheritdoc/>
        public T Item
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemResponse{T}"/> class.
        /// </summary>
        /// <param name="item">Content item of a specific type.</param>
        [JsonConstructor]
        internal DeliveryItemResponse(T item)
        {
            Item = item;
        }
    }
}
