using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.TaxonomyGroups
{
    /// <inheritdoc/>
    [DebuggerDisplay("Name = {" + nameof(System) + "." + nameof(IContentTypeSystemAttributes.Name) + "}")]
    public sealed class TaxonomyGroup : ITaxonomyGroup
    {
        private readonly JToken _source;
        private ITaxonomyGroupSystemAttributes _system;
        private IReadOnlyList<ITaxonomyTermDetails> _terms;

        /// <inheritdoc/>
        [JsonProperty("system")]
        public ITaxonomyGroupSystemAttributes System
        {
            get => _system ??= _source["system"].ToObject<TaxonomyGroupSystemAttributes>();
            set => _system = value;
        }

        /// <inheritdoc/>
        [JsonProperty("terms")]
        public IReadOnlyList<ITaxonomyTermDetails> Terms
        {
            get => _terms ??= _source["terms"].Select(term => new TaxonomyTermDetails(term)).ToList().AsReadOnly();
            set => _terms = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaxonomyGroup"/> class with the specified JSON data.
        /// </summary>
        /// <param name="source">The JSON data to deserialize.</param>
        internal TaxonomyGroup(JToken source)
        {
            //TODO: remove
            _source = source;
        }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        [JsonConstructor]
        internal TaxonomyGroup(ITaxonomyGroupSystemAttributes system, IReadOnlyList<ITaxonomyTermDetails> terms)
        {
            System = system;
            Terms = terms;
        }
    }
}
