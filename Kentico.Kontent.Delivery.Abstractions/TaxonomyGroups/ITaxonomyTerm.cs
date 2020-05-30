namespace Kentico.Kontent.Delivery.Abstractions.TaxonomyGroups
{
    /// <summary>
    /// Represents a taxonomy term assigned to a Taxonomy element.
    /// </summary>
    public interface ITaxonomyTerm
    {
        /// <summary>
        /// Gets the codename of the taxonomy term.
        /// </summary>
        string Codename { get; }

        /// <summary>
        /// Gets the name of the taxonomy term.
        /// </summary>
        string Name { get; }
    }
}