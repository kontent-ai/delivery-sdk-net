using System.Collections.Generic;
using System.Linq;
using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.Models.Taxonomy
{
    /// <summary>
    /// Represents a taxonomy term with child terms.
    /// </summary>
    public sealed class TaxonomyTermDetails : ITaxonomyTermDetails
    {
        private readonly JToken _source;
        private string _name;
        private string _codename;
        private IReadOnlyList<TaxonomyTermDetails> _terms;

        /// <summary>
        /// Gets the name of the taxonomy term.
        /// </summary>
        public string Name
            => _name ??= _source.Value<string>("name");

        /// <summary>
        /// Gets the codename of the taxonomy term.
        /// </summary>
        public string Codename
            => _codename ??= _source.Value<string>("codename");

        /// <summary>
        /// Gets a readonly collection that contains child terms of the taxonomy term.
        /// </summary>
        public IReadOnlyList<ITaxonomyTermDetails> Terms
            => _terms ??= _source["terms"].Select(term => new TaxonomyTermDetails(term)).ToList().AsReadOnly();

        /// <summary>
        /// Initializes a new instance of the <see cref="TaxonomyTermDetails"/> class.
        /// </summary>
        /// <param name="source">The JSON data to deserialize.</param>
        internal TaxonomyTermDetails(JToken source)
        {
            _source = source;
        }
    }
}
