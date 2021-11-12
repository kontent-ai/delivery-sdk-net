using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions.SharedModels;

namespace Kentico.Kontent.Delivery.Abstractions.TaxonomyGroups
{
    /// <summary>
    /// Represents a response from Kontent Delivery API that contains a list of taxonomy groups.
    /// </summary>
    public interface IDeliveryTaxonomyListingResponse : IResponse, IPageable
    {
        /// <summary>
        /// Gets a read-only list of taxonomy groups.
        /// </summary>
        IList<ITaxonomyGroup> Taxonomies { get; }
    }
}