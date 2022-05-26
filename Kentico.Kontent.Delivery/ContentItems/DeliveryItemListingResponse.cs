using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentItems
{
    /// <inheritdoc cref="IDeliveryItemListingResponse{T}" />
    internal sealed class DeliveryItemListingResponse<T> : AbstractItemsResponse, IDeliveryItemListingResponse<T>
    {
        /// <inheritdoc/>
        public IPagination Pagination
        {
            get;
        }

        /// <inheritdoc/>
        public IList<T> Items
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemListingResponse{T}"/> class.
        /// </summary>
        /// <param name="response">The response from Kontent Delivery API that contains content items.</param>
        /// <param name="items">A collection of content items of a specific type.</param>
        /// <param name="pagination">Response paging information.</param>
        [JsonConstructor]
        internal DeliveryItemListingResponse(ApiResponse response, IList<T> items,IPagination pagination) : base(response)
        {
            Items = items;
            Pagination = pagination;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemListingResponse{T}"/> class.
        /// </summary>
        /// <param name="response">The response from Kontent Delivery API that contains content items.</param>
        internal DeliveryItemListingResponse(ApiResponse response) : base(response)
        {
        }
    }
}
