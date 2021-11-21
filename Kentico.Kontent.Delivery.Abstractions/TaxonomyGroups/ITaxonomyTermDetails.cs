using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a taxonomy term with child terms.
    /// </summary>
    public interface ITaxonomyTermDetails
    {
        /// <summary>
        /// Gets the codename of the taxonomy term.
        /// </summary>
        string Codename { get; }

        /// <summary>
        /// Gets the name of the taxonomy term.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a readonly collection that contains child terms of the taxonomy term.
        /// </summary>
        IList<ITaxonomyTermDetails> Terms { get; }
    }
}