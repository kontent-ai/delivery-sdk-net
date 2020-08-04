using System.Collections.Generic;
using System.Diagnostics;
using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;

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
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        [JsonConstructor]
        public TaxonomyTermDetails()
        {
        }
    }
}
