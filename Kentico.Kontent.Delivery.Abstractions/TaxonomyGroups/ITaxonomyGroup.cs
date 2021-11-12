using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions.TaxonomyGroups
{
    /// <summary>
    /// Represents a taxonomy group.
    /// </summary>
    public interface ITaxonomyGroup
    {
        /// <summary>
        /// Gets the system attributes of the taxonomy group.
        /// </summary>
        ITaxonomyGroupSystemAttributes System { get; }

        /// <summary>
        /// Gets a readonly collection that contains terms of the taxonomy group.
        /// </summary>
        IList<ITaxonomyTermDetails> Terms { get; }
    }
}