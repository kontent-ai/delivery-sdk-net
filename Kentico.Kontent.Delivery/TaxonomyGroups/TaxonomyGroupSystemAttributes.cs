using System;
using System.Diagnostics;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.TaxonomyGroups;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.TaxonomyGroups
{
    /// <inheritdoc/>
    [DebuggerDisplay("Id = {" + nameof(Id) + "}")]
    internal sealed class TaxonomyGroupSystemAttributes : ITaxonomyGroupSystemAttributes
    {
        /// <inheritdoc/>
        [JsonProperty("id")]
        public string Id { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("codename")]
        public string Codename { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty("last_modified")]
        public DateTime LastModified { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaxonomyGroupSystemAttributes"/> class.
        /// </summary>
        [JsonConstructor]
        public TaxonomyGroupSystemAttributes()
        {
        }
    }
}
