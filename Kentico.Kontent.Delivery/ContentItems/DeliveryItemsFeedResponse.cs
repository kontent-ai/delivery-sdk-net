using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.SharedModels;

namespace Kentico.Kontent.Delivery.ContentItems
{
    /// <inheritdoc cref="IDeliveryItemsFeedResponse{T}" />
    internal class DeliveryItemsFeedResponse<T> : AbstractItemsResponse, IDeliveryItemsFeedResponse<T>
    {
        /// <inheritdoc/>
        public IList<T> Items { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemsFeedResponse{T}"/> class.
        /// </summary>
        /// <param name="response">The response from Kontent Delivery API that contains a list of content items.</param>
        /// <param name="items">A list of content items.</param>
        internal DeliveryItemsFeedResponse(ApiResponse response, IList<T> items) : base(response)
        {
            Items = items;
        }
    }
}