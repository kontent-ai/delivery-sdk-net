namespace Kontent.Ai.Delivery.Abstractions
{
    interface ITaxonomyElement : IContentElement
    {
        /// <summary>
        /// Gets the codename of the taxonomy group for the Taxonomy content element; otherwise, an empty string.
        /// </summary>
        string TaxonomyGroup { get; }
    }
}
