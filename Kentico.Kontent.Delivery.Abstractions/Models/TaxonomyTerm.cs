using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <summary>
    /// Represents a taxonomy term assigned to a Taxonomy element.
    /// </summary>
    public sealed class TaxonomyTerm
    {
        /// <summary>
        /// Gets the name of the taxonomy term.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; }

        /// <summary>
        /// Gets the codename of the taxonomy term.
        /// </summary>
        [JsonProperty("codename")]
        public string Codename { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaxonomyTerm"/> class.
        /// </summary>
        [JsonConstructor]
        internal TaxonomyTerm(string name, string codename)
        {
            Name = name;
            Codename = codename;
        }
    }
}
