using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.TaxonomyGroups
{
    /// <inheritdoc/>
    [DebuggerDisplay("Name = {" + nameof(Name) + "}")]
    internal sealed class TaxonomyTermDetails : ITaxonomyTermDetails
    {
        private readonly JToken _source;
        private string _name;
        private string _codename;
        private IReadOnlyList<ITaxonomyTermDetails> _terms;

        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name
        {
            get => _name ??= _source.Value<string>("name");
            set => _name = value;
        }

        /// <inheritdoc/>
        [JsonProperty("codename")]
        public string Codename
        {
            get => _codename ??= _source.Value<string>("codename");
            set => _codename = value;
        }

        /// <inheritdoc/>
        [JsonProperty("terms")]
        public IReadOnlyList<ITaxonomyTermDetails> Terms
        {
            get => _terms ??= _source["terms"].Select(term => new TaxonomyTermDetails(term)).ToList().AsReadOnly();
            set => _terms = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaxonomyTermDetails"/> class.
        /// </summary>
        /// <param name="source">The JSON data to deserialize.</param>
        public TaxonomyTermDetails(JToken source)
        {
            _source = source;
        }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        [JsonConstructor]
        public TaxonomyTermDetails(string name, string codename, IReadOnlyList<ITaxonomyTermDetails> terms)
        {
            Name = name;
            Codename = codename;
            Terms = terms;
        }
    }
}
