using System;
using System.Collections.Generic;
using System.Threading;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentTypes
{
    /// <inheritdoc cref="IDeliveryTypeListingResponse" />
    internal sealed class DeliveryTypeListingResponse : AbstractResponse, IDeliveryTypeListingResponse
    {
        private Lazy<IPagination> _pagination;
        private Lazy<IReadOnlyList<IContentType>> _types;

        /// <inheritdoc/>
        public IPagination Pagination
        {
            get => _pagination.Value;
            private set => _pagination = new Lazy<IPagination>(() => value);
        }

        /// <inheritdoc/>
        public IReadOnlyList<IContentType> Types
        {
            get => _types.Value;
            private set => _types = new Lazy<IReadOnlyList<IContentType>>(() => value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTypeListingResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a list of content types.</param>
        internal DeliveryTypeListingResponse(ApiResponse response) : base(response)
        {
            _pagination = new Lazy<IPagination>(() => response.JsonContent["pagination"].ToObject<Pagination>(), LazyThreadSafetyMode.PublicationOnly);
            _types = new Lazy<IReadOnlyList<IContentType>>(() => response.JsonContent["types"].ToObject<IReadOnlyList<ContentType>>(Serializer), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
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
