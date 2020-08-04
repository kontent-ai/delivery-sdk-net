using System.Collections.Generic;
using System.Diagnostics;
using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.TaxonomyGroups
{
    /// <inheritdoc/>
    [DebuggerDisplay("Name = {" + nameof(System) + "." + nameof(IContentTypeSystemAttributes.Name) + "}")]
    public sealed class TaxonomyGroup : ITaxonomyGroup
    {
        /// <inheritdoc/>
        [JsonProperty("system")]
        public ITaxonomyGroupSystemAttributes System { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("terms")]
        public IReadOnlyList<ITaxonomyTermDetails> Terms { get; internal set; }

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
