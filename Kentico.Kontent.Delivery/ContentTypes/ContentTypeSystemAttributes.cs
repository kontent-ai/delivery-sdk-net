using System;
using System.Diagnostics;
using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentTypes
{
    /// <inheritdoc/>
    [DebuggerDisplay("Id = {" + nameof(Id) + "}")]
    public sealed class ContentTypeSystemAttributes : IContentTypeSystemAttributes
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
