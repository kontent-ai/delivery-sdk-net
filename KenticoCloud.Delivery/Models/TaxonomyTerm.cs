using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a taxonomy term.
    /// </summary>
    public class TaxonomyTerm
    {
        /// <summary>
        /// Gets or sets the name of the taxonomy term.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the codename of the taxonomy term.
        /// </summary>
        public string Codename { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaxonomyTerm"/> class with the specified JSON data.
        /// </summary>
        /// <param name="term">The JSON data to deserialize.</param>
        public TaxonomyTerm(JToken term)
        {
            Name = term["name"].ToString();
            Codename = term["codename"].ToString();
        }
    }
}