using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentTypes
{
    /// <inheritdoc cref="IDeliveryTypeListingResponse" />
    internal sealed class DeliveryTypeListingResponse : IDeliveryTypeListingResponse
    {
        /// <inheritdoc/>
        public IPagination Pagination
        {
            get;
        }

        /// <inheritdoc/>
        public IList<IContentType> Types
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTypeListingResponse"/> class.
        /// </summary>
        /// <param name="types">A collection of content types.</param>
        /// <param name="pagination">Response paging information.</param>
        [JsonConstructor]
        internal DeliveryTypeListingResponse(IList<IContentType> types, IPagination pagination)
        {
            Types = types;
            Pagination = pagination;
        }
    }
}
