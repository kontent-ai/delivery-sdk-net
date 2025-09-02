namespace Kontent.Ai.Delivery.ContentItems
{
    /// <inheritdoc cref="IDeliveryItemsFeedResponse{T}" />
    internal class DeliveryItemsFeedResponse<T> : IDeliveryItemsFeedResponse<T>
    {
        /// <inheritdoc/>
        public IList<T> Items { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemsFeedResponse{T}"/> class.
        /// </summary>
        /// <param name="items">A list of content items.</param>
        internal DeliveryItemsFeedResponse(IList<T> items)
        {
            Items = items;
        }
    }
}