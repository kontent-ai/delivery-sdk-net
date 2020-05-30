using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.Responses;
using Kentico.Kontent.Delivery.Models.Taxonomy;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Kentico.Kontent.Delivery.Models
{
    /// <summary>
    /// Represents a response from Kentico Kontent Delivery API that contains a list of taxonomy groups.
    /// </summary>
    public sealed class DeliveryTaxonomyListingResponse : AbstractResponse, IDeliveryTaxonomyListingResponse
    {
        private readonly Lazy<Pagination> _pagination;
        private readonly Lazy<IReadOnlyList<TaxonomyGroup>> _taxonomies;

        /// <summary>
        /// Gets paging information.
        /// </summary>
        public IPagination Pagination => _pagination.Value;

        /// <summary>
        /// Gets a read-only list of taxonomy groups.
        /// </summary>
        public IReadOnlyList<ITaxonomyGroup> Taxonomies => _taxonomies.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTaxonomyListingResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a list of taxonomy groups.</param>
        internal DeliveryTaxonomyListingResponse(ApiResponse response) : base(response)
        {
            _pagination = new Lazy<Pagination>(() => response.JsonContent["pagination"].ToObject<Pagination>(), LazyThreadSafetyMode.PublicationOnly);
            _taxonomies = new Lazy<IReadOnlyList<TaxonomyGroup>>(() => ((JArray)response.JsonContent["taxonomies"]).Select(source => new TaxonomyGroup(source)).ToList().AsReadOnly(), LazyThreadSafetyMode.PublicationOnly);
        }
    }
}
