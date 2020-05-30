using System;

namespace Kentico.Kontent.Delivery.Abstractions.TaxonomyGroups
{
    /// <summary>
    /// Represents system attributes of a taxonomy group
    /// </summary>
    public interface ITaxonomyGroupSystemAttributes
    {
        /// <summary>
        /// Gets the codename of the taxonomy group.
        /// </summary>
        string Codename { get; }

        /// <summary>
        /// Gets the identifier of the taxonomy group.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the time the taxonomy group was last modified.
        /// </summary>
        DateTime LastModified { get; }

        /// <summary>
        /// Gets the name of the taxonomy group.
        /// </summary>
        string Name { get; }
    }
}