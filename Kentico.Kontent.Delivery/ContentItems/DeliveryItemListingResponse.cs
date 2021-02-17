using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        /// <param name="response">The response from Kentico Kontent Delivery API that contains content items.</param>
        /// <param name="items">A collection of content items of a specific type.</param>
        /// <param name="linkedItems">A delegate to resolve linked items.</param>
        /// <param name="pagination">Response paging information.</param>
        [JsonConstructor]
        internal DeliveryItemListingResponse(ApiResponse response, IList<T> items, Func<Task<IList<object>>> linkedItems, IPagination pagination) : base(response, linkedItems)
        {
            Items = items;
            Pagination = pagination;
        }
    }
}
