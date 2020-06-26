using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.TaxonomyGroups
{
    /// <summary>
    /// Represents a response from Kentico Kontent Delivery API that contains a list of taxonomy groups.
    /// </summary>
    internal sealed class DeliveryTaxonomyListingResponse : AbstractResponse, IDeliveryTaxonomyListingResponse
    {
        private Lazy<IPagination> _pagination;
        private Lazy<IReadOnlyList<ITaxonomyGroup>> _taxonomies;

        /// <summary>
        /// Gets paging information.
        /// </summary>
        public IPagination Pagination
        {
            get => _pagination.Value;
            private set => _pagination = new Lazy<IPagination>(() => value);
        }

        /// <summary>
        /// Gets a read-only list of taxonomy groups.
        /// </summary>
        public IReadOnlyList<ITaxonomyGroup> Taxonomies
        {
            get => _taxonomies.Value;
            private set => _taxonomies = new Lazy<IReadOnlyList<ITaxonomyGroup>>(() => value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTaxonomyListingResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains a list of taxonomy groups.</param>
        internal DeliveryTaxonomyListingResponse(ApiResponse response) : base(response)
        {
            _pagination = new Lazy<IPagination>(() => response.JsonContent["pagination"].ToObject<Pagination>(), LazyThreadSafetyMode.PublicationOnly);
            _taxonomies = new Lazy<IReadOnlyList<ITaxonomyGroup>>(() => ((JArray)response.JsonContent["taxonomies"]).Select(source => new TaxonomyGroup(source)).ToList().AsReadOnly(), LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains taxonomies.</param>
        /// <param name="taxonomies">A collection of taxonomies.</param>
        /// <param name="pagination">Response paging information.</param>
        [JsonConstructor]
        internal DeliveryTaxonomyListingResponse(ApiResponse response, IReadOnlyList<ITaxonomyGroup> taxonomies, IPagination pagination) : base(response)
        {
            Taxonomies = taxonomies;
            Pagination = pagination;
        }
    }
}
