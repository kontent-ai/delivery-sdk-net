using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.Abstractions
{
    /// <inheritdoc/>
    public sealed class TaxonomyTerm : ITaxonomyTerm
    {
        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name { get; }

        /// <inheritdoc/>
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
