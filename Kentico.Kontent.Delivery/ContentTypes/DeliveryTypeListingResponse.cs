using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentTypes
{
    /// <inheritdoc cref="IDeliveryTypeListingResponse" />
    internal sealed class DeliveryTypeListingResponse : AbstractResponse, IDeliveryTypeListingResponse
    {
        /// <inheritdoc/>
        public IPagination Pagination
        {
            get;
            private set;
        }

        /// <inheritdoc/>
        public IReadOnlyList<IContentType> Types
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTypeListingResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains content types.</param>
        /// <param name="types">A collection of content types.</param>
        /// <param name="pagination">Response paging information.</param>
        [JsonConstructor]
        internal DeliveryTypeListingResponse(ApiResponse response, IReadOnlyList<IContentType> types, IPagination pagination) : base(response)
        {
            Types = types;
            Pagination = pagination;
        }
    }
}
