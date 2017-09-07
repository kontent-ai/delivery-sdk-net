using System;
using Newtonsoft.Json;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents system attributes of a taxonomy group
    /// </summary>
    public sealed class TaxonomyGroupSystemAttributes
    {
        /// <summary>
        /// Gets the identifier of the taxonomy group.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; }

        /// <summary>
        /// Gets the name of the taxonomy group.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; }

        /// <summary>
        /// Gets the codename of the taxonomy group.
        /// </summary>
        [JsonProperty("codename")]
        public string Codename { get; }

        /// <summary>
        /// Gets the time the taxonomy group was last modified.
        /// </summary>
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
