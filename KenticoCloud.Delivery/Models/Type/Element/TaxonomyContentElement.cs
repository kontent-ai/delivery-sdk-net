using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a Taxonomy content element.
    /// </summary>
    public sealed class TaxonomyContentElement : ContentElement
    {
        /// <summary>
        /// Gets the codename of the taxonomy group.
        /// </summary>
        public string TaxonomyGroup { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaxonomyContentElement"/> class with the specified JSON data.
        /// </summary>
        /// <param name="source">The JSON data to deserialize.</param>
        /// <param name="codename">The codename of the content element.</param>
        internal TaxonomyContentElement(JToken source, string codename) : base(source, codename)
        {
            TaxonomyGroup = source["taxonomy_group"].ToString();
        }
    }
}
