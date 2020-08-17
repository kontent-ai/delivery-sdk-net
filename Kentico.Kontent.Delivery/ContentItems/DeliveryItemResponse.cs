using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentItems
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
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a content item.</param>
        /// <param name="item">Content item of a specific type.</param>
        /// <param name="linkedItems">Collection of linked content items.</param>
        [JsonConstructor]
        internal DeliveryItemResponse(ApiResponse response, T item, IList<object> linkedItems) : base(response, linkedItems)
        {
            Item = item;
        }

        /// <summary>
        /// Implicitly converts the specified <paramref name="response"/> to a content item.
        /// </summary>
        /// <param name="response">The response to convert.</param>
        public static implicit operator T(DeliveryItemResponse<T> response) => response.Item;
    }
}
