using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.TaxonomyGroups
{
    /// <summary>
    /// Represents a response from Kontent.ai Delivery API that contains a list of taxonomy groups.
    /// </summary>
    internal sealed class DeliveryTaxonomyListingResponse : IDeliveryTaxonomyListingResponse
    {
        /// <summary>
        /// Gets paging information.
        /// </summary>
        public IPagination Pagination
        {
            get;
        }

        /// <summary>
        /// Gets a read-only list of taxonomy groups.
        /// </summary>
        public IList<ITaxonomyGroup> Taxonomies
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryTaxonomyListingResponse"/> class.
        /// </summary>
        /// <param name="taxonomies">A collection of taxonomies.</param>
        /// <param name="pagination">Response paging information.</param>
        [JsonConstructor]
        internal DeliveryTaxonomyListingResponse(IList<ITaxonomyGroup> taxonomies, IPagination pagination)
        {
            Taxonomies = taxonomies;
            Pagination = pagination;
        }
    }
}
