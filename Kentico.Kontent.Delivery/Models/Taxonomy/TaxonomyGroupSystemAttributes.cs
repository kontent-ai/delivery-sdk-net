using System;
using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.Models.Taxonomy
{
    /// <inheritdoc/>
    public sealed class TaxonomyGroupSystemAttributes : ITaxonomyGroupSystemAttributes
    {
        /// <inheritdoc/>
        [JsonProperty("id")]
        public string Id { get; }

        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name { get; }

        /// <inheritdoc/>
        [JsonProperty("codename")]
        public string Codename { get; }

        /// <inheritdoc/>
        [JsonProperty("last_modified")]
        public DateTime LastModified { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaxonomyGroupSystemAttributes"/> class.
        /// </summary>
        [JsonConstructor]
        internal TaxonomyGroupSystemAttributes(string id, string name, string codename, DateTime lastModified)
        {
            Id = id;
            Name = name;
            Codename = codename;
            LastModified = lastModified;
        }
    }
}
