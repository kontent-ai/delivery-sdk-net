using System.Collections.Generic;
using System.Diagnostics;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.ContentTypes;
using Kentico.Kontent.Delivery.Abstractions.TaxonomyGroups;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.TaxonomyGroups
{
    /// <inheritdoc/>
    [DebuggerDisplay("Name = {" + nameof(System) + "." + nameof(IContentTypeSystemAttributes.Name) + "}")]
    internal sealed class TaxonomyGroup : ITaxonomyGroup
    {
        /// <inheritdoc/>
        [JsonProperty("system")]
        public ITaxonomyGroupSystemAttributes System { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("terms")]
        public IList<ITaxonomyTermDetails> Terms { get; internal set; }

        /// <summary>
        /// Constructor used for deserialization (e.g. for caching purposes), contains no logic.
        /// </summary>
        [JsonConstructor]
        public TaxonomyGroup(ITaxonomyGroupSystemAttributes system, IList<ITaxonomyTermDetails> terms)
        {
            System = system;
            Terms = terms;
        }
    }
}
