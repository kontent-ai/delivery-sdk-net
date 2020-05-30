using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions.Models;
using Kentico.Kontent.Delivery.Abstractions.Models.Taxonomy;

namespace Kentico.Kontent.Delivery.Abstractions.Responses
{
    /// <summary>
    /// Represents a response from Kentico Kontent Delivery API that contains a list of taxonomy groups.
    /// </summary>
    public interface IDeliveryTaxonomyListingResponse : IResponse
    {
        /// <summary>
        /// Gets paging information.
        /// </summary>
        IPagination Pagination { get; }

        /// <summary>
        /// Gets a read-only list of taxonomy groups.
        /// </summary>
        IReadOnlyList<ITaxonomyGroup> Taxonomies { get; }
    }
}