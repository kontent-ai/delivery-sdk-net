using Kontent.Ai.Delivery.SharedModels;
using System.Text.Json.Serialization;
using IApiResponse = Kontent.Ai.Delivery.Abstractions.IApiResponse;

namespace Kontent.Ai.Delivery.ContentItems
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
        /// <param name="response">The response from Kontent.ai Delivery API that contains content items.</param>
        /// <param name="items">A collection of content items of a specific type.</param>
        /// <param name="pagination">Response paging information.</param>
        [JsonConstructor]
        internal DeliveryItemListingResponse(IApiResponse response, IList<T> items, IPagination pagination) : base(response)
        {
            Items = items;
            Pagination = pagination;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemListingResponse{T}"/> class.
        /// </summary>
        /// <param name="response">The response from Kontent.ai Delivery API that contains content items.</param>
        internal DeliveryItemListingResponse(IApiResponse response) : base(response)
        {
        }
    }
}
