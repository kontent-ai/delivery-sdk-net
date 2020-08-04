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
        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("codename")]
        public string Codename { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("terms")]
        public IReadOnlyList<ITaxonomyTermDetails> Terms { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaxonomyTermDetails"/> class.
        /// </summary>
        /// <param name="source">The JSON data to deserialize.</param>
        public TaxonomyTermDetails(JToken source)
        {
            Name = source.Value<string>("name");
            Codename = source.Value<string>("codename");
            Terms = source["terms"].Select(term => new TaxonomyTermDetails(term)).ToList().AsReadOnly();
        }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        [JsonConstructor]
        public TaxonomyTermDetails()
        {
        }
    }
}
