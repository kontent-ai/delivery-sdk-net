using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Kontent Delivery API that contains a list of content types.
    /// </summary>
    public sealed class DeliveryTypeListingResponse : AbstractResponse
    {
        private readonly Lazy<Pagination> _pagination;
        private readonly Lazy<IReadOnlyList<ContentType>> _types;

        /// <summary>
        /// Gets paging information.
        /// </summary>
        public Pagination Pagination => _pagination.Value;

        /// <summary>
        /// Gets a read-only list of content types.
        /// </summary>
        public IReadOnlyList<ContentType> Types => _types.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTypeListingResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a list of content types.</param>
        internal DeliveryTypeListingResponse(ApiResponse response) : base(response)
        {
            _pagination = new Lazy<Pagination>(() => _response.Content["pagination"].ToObject<Pagination>(), LazyThreadSafetyMode.PublicationOnly);
            _types = new Lazy<IReadOnlyList<ContentType>>(() => ((JArray)_response.Content["types"]).Select(source => new ContentType(source)).ToList().AsReadOnly(), LazyThreadSafetyMode.PublicationOnly);
        }
    }
}
