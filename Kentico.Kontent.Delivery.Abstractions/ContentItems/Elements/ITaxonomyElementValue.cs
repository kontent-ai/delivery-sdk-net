using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions.SharedModels;

namespace Kentico.Kontent.Delivery.Abstractions.ContentItems.Elements
{
    /// <summary>
    /// An object representing a taxonomy element value.
    /// </summary>
    public interface ITaxonomyElementValue : IContentElementValue<IEnumerable<ITaxonomyTerm>>
    {
        /// <summary>
        /// Gets the codename of the taxonomy group for the Taxonomy content element; otherwise, an empty string.
        /// </summary>
        string TaxonomyGroup { get; }
    }
}
