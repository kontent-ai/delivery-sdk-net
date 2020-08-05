using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.SharedModels;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.TaxonomyGroups
{
    /// <summary>
    /// Represents a response from Kentico Kontent Delivery API that contains a list of taxonomy groups.
    /// </summary>
    internal sealed class DeliveryTaxonomyListingResponse : AbstractResponse, IDeliveryTaxonomyListingResponse
    {
        /// <summary>
        /// Gets paging information.
        /// </summary>
        public IPagination Pagination
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a read-only list of taxonomy groups.
        /// </summary>
        public IList<ITaxonomyGroup> Taxonomies
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTaxonomyListingResponse"/> class.
        /// </summary>
        /// <param name="response">The response from Kentico Kontent Delivery API that contains taxonomies.</param>
        /// <param name="taxonomies">A collection of taxonomies.</param>
        /// <param name="pagination">Response paging information.</param>
        [JsonConstructor]
        internal DeliveryTaxonomyListingResponse(ApiResponse response, IList<ITaxonomyGroup> taxonomies, IPagination pagination) : base(response)
        {
            Taxonomies = taxonomies;
            Pagination = pagination;
        }
    }
}
