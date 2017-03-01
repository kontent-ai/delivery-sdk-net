using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a taxonomy term assigned to a Taxonomy element.
    /// </summary>
    public sealed class TaxonomyTerm
    {
        /// <summary>
        /// Gets the name of the taxonomy term.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the codename of the taxonomy term.
        /// </summary>
        public string Codename { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaxonomyTerm"/> class with the specified JSON data.
        /// </summary>
        [JsonConstructor]
        internal TaxonomyTerm(string name, string codename)
        {
            Name = name;
            Codename = codename;
        }
    }
}
