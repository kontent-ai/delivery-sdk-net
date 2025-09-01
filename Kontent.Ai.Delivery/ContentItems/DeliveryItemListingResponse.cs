using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems
{
    /// <inheritdoc cref="IDeliveryItemListingResponse{T}" />
    internal sealed class DeliveryItemListingResponse<T> : IDeliveryItemListingResponse<T>
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
        /// <param name="items">A collection of content items of a specific type.</param>
        /// <param name="pagination">Response paging information.</param>
        [JsonConstructor]
        internal DeliveryItemListingResponse(IList<T> items, IPagination pagination)
        {
            Items = items;
            Pagination = pagination;
        }
    }
}
