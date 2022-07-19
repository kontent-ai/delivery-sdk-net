using System;
using System.Diagnostics;
using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.ContentTypes
{
    /// <inheritdoc/>
    [DebuggerDisplay("Id = {" + nameof(Id) + "}")]
    internal sealed class ContentTypeSystemAttributes : IContentTypeSystemAttributes
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
        /// Initializes a new instance of the <see cref="ContentTypeSystemAttributes"/> class.
        /// </summary>
        [JsonConstructor]
        public ContentTypeSystemAttributes()
        {
        }
    }
}
