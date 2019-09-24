using Newtonsoft.Json;
using System;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// Represents system attributes of a content type.
    /// </summary>
    public sealed class ContentTypeSystemAttributes
    {
        /// <summary>
        /// Gets the identifier of the content type.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; }

        /// <summary>
        /// Gets the name of the content type.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; }

        /// <summary>
        /// Gets the codename of the content type.
        /// </summary>
        [JsonProperty("codename")]
        public string Codename { get; }

        /// <summary>
        /// Gets the time the content type was last modified.
        /// </summary>
        [JsonProperty("last_modified")]
        public DateTime LastModified { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentTypeSystemAttributes"/> class.
        /// </summary>
        [JsonConstructor]
        internal ContentTypeSystemAttributes(string id, string name, string codename, DateTime lastModified)
        {
            Id = id;
            Name = name;
            Codename = codename;
            LastModified = lastModified;
        }
    }
}
