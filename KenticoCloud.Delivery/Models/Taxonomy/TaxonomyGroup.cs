using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a taxonomy group.
    /// </summary>
    public sealed class TaxonomyGroup
    {
        private readonly JToken _source;
        private TaxonomyGroupSystemAttributes _system;
        private IReadOnlyList<TaxonomyTermDetails> _terms;

        /// <summary>
        /// Gets the system attributes of the taxonomy group.
        /// </summary>
        public TaxonomyGroupSystemAttributes System
            => _system ?? (_system = _source["system"].ToObject<TaxonomyGroupSystemAttributes>());

        /// <summary>
        /// Gets a readonly collection that contains terms of the taxonomy group.
        /// </summary>
        public IReadOnlyList<TaxonomyTermDetails> Terms
            => _terms ?? (_terms = _source["terms"].Select(term => new TaxonomyTermDetails(term)).ToList().AsReadOnly());

        /// <summary>
        /// Initializes a new instance of the <see cref="TaxonomyGroup"/> class with the specified JSON data.
        /// </summary>
        /// <param name="source">The JSON data to deserialize.</param>
        internal TaxonomyGroup(JToken source)
        {
            _source = source;
        }
    }
}
