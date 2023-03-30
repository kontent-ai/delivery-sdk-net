using System.Collections.Generic;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.ContentTypes
{
    /// <inheritdoc cref="IDeliveryTypeListingResponse" />
    internal sealed class DeliveryTypeListingResponse : AbstractResponse, IDeliveryTypeListingResponse
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
        /// <param name="response">The response from Kontent.ai Delivery API that contains content types.</param>
        /// <param name="types">A collection of content types.</param>
        /// <param name="pagination">Response paging information.</param>
        [JsonConstructor]
        internal DeliveryTypeListingResponse(IApiResponse response, IList<IContentType> types, IPagination pagination) : base(response)
        {
            Types = types;
            Pagination = pagination;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTypeListingResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kontent.ai Delivery API that contains content types.</param>
        internal DeliveryTypeListingResponse(IApiResponse response) : base(response)
        {
        }
    }
}
